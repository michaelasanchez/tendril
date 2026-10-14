import cn from 'classnames';
import { differenceInDays, format, startOfDay } from 'date-fns';
import React, { useCallback, useEffect, useState } from 'react';
import { Badge, Col, Row, Spinner } from 'react-bootstrap';
import { useNavigate } from 'react-router';
import { CategoriesApi } from '../../api/categories';
import { EventsApi } from '../../api/events';
import { ScrapersApi } from '../../api/scrapers';
import { SquareButton as Button } from '../../components/button';
import { FormDate, FormSelect } from '../../components/form';
import { Icon } from '../../components/Icon';
import { ReviewEventCard } from '../../events';
import { pageStyles } from '../../styles';
import type {
  Category,
  Guid,
  PendingEventReviewDto,
  ScraperDefinition,
} from '../../types/api';
import styles from './ReviewPage.module.css';

interface EventFilter {
  from: string | null;
  scraperId: Guid | null;
}

export const ReviewPage: React.FC = () => {
  // Use the updated TypeScript interface name representing the new C# DTO wrapper
  const [reviewPayloads, setReviewPayloads] = useState<PendingEventReviewDto[]>(
    [],
  );
  const [categories, setCategories] = useState<Category[]>([]);
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [loading, setLoading] = useState(true);

  const [filter, setFilter] = useState<EventFilter>({
    from: format(new Date(), 'yyyy-MM-dd'),
    scraperId: null,
  });

  const navigate = useNavigate();

  const loadData = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    try {
      // Calls your updated C# [HttpGet("pending-review")]
      const [reviewData, categoriesData, scrapersData] = await Promise.all([
        EventsApi.getPending(signal),
        CategoriesApi.getAll(signal),
        ScrapersApi.getAll(signal),
      ]);
      setReviewPayloads(reviewData);
      setCategories(
        categoriesData.sort((a, b) => a.name.localeCompare(b.name)),
      );
      setScrapers(scrapersData);
    } catch (e) {
      console.error('Failed to sync review portal', e);
    } finally {
      setLoading(false);
    }
  }, []);

  const reloadEvent = useCallback(
    async (eventId: Guid, signal?: AbortSignal) => {
      try {
        const updated = await EventsApi.getById(eventId, signal);
        setReviewPayloads((prev) =>
          prev.map((i) =>
            i.pendingEvent.id == eventId ? { ...i, pendingEvent: updated } : i,
          ),
        );
      } catch (e) {
        console.error('Failed to reload event', e);
      }
    },
    [],
  );

  useEffect(() => {
    const controller = new AbortController();
    loadData(controller.signal);
    return () => controller.abort();
  }, [loadData]);

  return (
    <section className="container-fluid py-4">
      <div className={pageStyles.pageHeader}>
        <h2>
          Pending Review Pipeline (
          {loading ? (
            <Spinner animation="border" variant="light" size="sm" />
          ) : (
            reviewPayloads.length
          )}
          )
        </h2>
        <div className="d-flex gap-3">
          <FormSelect
            label="Scraper"
            value={filter.scraperId ?? ''}
            onChange={(scraperId) => setFilter((f) => ({ ...f, scraperId }))}
            options={[
              { value: '', label: '' },
              ...scrapers.map((s) => ({ value: s.id, label: s.name })),
            ]}
          />
          <FormDate
            inline
            label="From"
            value={filter.from ?? ''}
            onChange={(from) => setFilter((f) => ({ ...f, from }))}
          />
        </div>
      </div>

      <div className="d-flex flex-column gap-4">
        {reviewPayloads
          .filter((r) => {
            const startDate =
              r.pendingEvent.startDateTime ?? r.pendingEvent.startDate;

            return (
              (!filter.from ||
                (startDate &&
                  startOfDay(new Date(startDate)) >=
                    new Date(filter.from ?? ''))) &&
              (!filter.scraperId || r.scraperId === filter.scraperId)
            );
          })
          .map(({ pendingEvent, potentialMatches, scraperId }) => {
            const hasConflict = potentialMatches && potentialMatches.length > 0;
            const isPending = pendingEvent.status === 'Pending';

            return (
              <div
                key={pendingEvent.id}
                className={cn(styles.reviewRowGroup, {
                  [styles.conflictDetected]: hasConflict,
                })}
              >
                {hasConflict && (
                  <div className={styles.conflictHeader}>
                    <Icon name="warning" /> Conflict Identified: This looks like
                    an update to an existing live event
                  </div>
                )}

                <Row className="g-3 align-items-stretch">
                  {/* Left Side: The Raw Incoming Scraped Event */}
                  <Col lg={hasConflict ? 6 : 12}>
                    <div className="position-relative h-100">
                      <div className="d-flex gap-1 align-items-center">
                        <Badge
                          bg={isPending ? 'warning' : 'success'}
                          text={isPending ? 'dark' : 'light'}
                          className={styles.statusBadge}
                        >
                          {isPending ? 'INCOMING PENDING' : 'PUBLISHED'} (From:{' '}
                          {scrapers.find((s) => s.id === scraperId)?.name ??
                            'Unknown Scraper'}
                          )
                        </Badge>
                        <Button
                          size="xxs"
                          variant={
                            isPending ? 'outline-warning' : 'outline-success'
                          }
                          onClick={() =>
                            navigate(`/admin/scrapers/${scraperId}/output`)
                          }
                        >
                          <Icon name="external" />
                        </Button>
                      </div>
                      <ReviewEventCard e={pendingEvent} />
                      <div>
                        <small>
                          <strong>Scraped:</strong>{' '}
                          {format(
                            pendingEvent.scrapedAtUtc,
                            'MM-dd-yy h:mm:ss',
                          )}
                        </small>
                      </div>
                    </div>
                  </Col>

                  {/* Right Side: The Existing Published Candidate Event(s) */}
                  {hasConflict && (
                    <Col lg={6}>
                      <div className="d-flex flex-column gap-2 h-100 justify-content-center">
                        {potentialMatches.map((existingEvent) => (
                          <>
                            <div
                              key={existingEvent.id}
                              className="position-relative opacity-75"
                            >
                              <Badge
                                bg={
                                  existingEvent.status === 'Suppressed'
                                    ? 'danger'
                                    : 'success'
                                }
                                className={styles.statusBadge}
                              >
                                {existingEvent.status === 'Suppressed'
                                  ? 'SUPPRESSED (ID: '
                                  : 'LIVE PUBLISHED (ID: '}
                                {existingEvent.id.substring(0, 5)})
                              </Badge>
                              <ReviewEventCard e={existingEvent} />
                            </div>
                            <div>
                              <small>
                                <strong>Scraped:</strong>{' '}
                                {format(
                                  existingEvent.scrapedAtUtc,
                                  'MM-dd-yy h:mm:ss',
                                )}
                                <div>
                                  {pendingEvent.scrapedAtUtc >
                                  existingEvent.scrapedAtUtc
                                    ? `${differenceInDays(
                                        pendingEvent.scrapedAtUtc,
                                        existingEvent.scrapedAtUtc,
                                      )} day(s) earlier`
                                    : `${differenceInDays(
                                        existingEvent.scrapedAtUtc,
                                        pendingEvent.scrapedAtUtc,
                                      )} day(s) later`}
                                </div>
                              </small>
                            </div>
                          </>
                        ))}
                      </div>
                    </Col>
                  )}
                </Row>

                {/* Universal Action Toolbar positioned cleanly underneath the cards */}
                <div className="d-flex justify-content-between align-items-center mt-3 pt-2 border-top border-secondary">
                  <div style={{ width: '200px' }}>
                    <FormSelect
                      value={
                        categories?.find(
                          (c) => c.name === pendingEvent.categoryName,
                        )?.id ?? ''
                      }
                      options={[
                        { value: '', label: 'Select Category' },
                        ...categories.map((c) => ({
                          value: c.id,
                          label: c.name,
                        })),
                      ]}
                      onChange={(val) =>
                        EventsApi.patch(pendingEvent.id, {
                          categoryId: val,
                        }).then(() => reloadEvent(pendingEvent.id))
                      }
                    />
                  </div>

                  <div className="d-flex gap-2">
                    <Button
                      className="btn-success"
                      onClick={() =>
                        EventsApi.patch(pendingEvent.id, {
                          status: 'Published',
                        }).then(() => reloadEvent(pendingEvent.id))
                      }
                    >
                      <Icon name="publish" /> Approve & Publish New
                    </Button>

                    {hasConflict && (
                      <Button
                        className="btn-info"
                        onClick={() =>
                          EventsApi.supersedeAndPublish(
                            pendingEvent.id,
                            potentialMatches[0].id,
                          )
                            // TODO: error is thrown because no JSON is returned from the API
                            .finally(() => {
                              potentialMatches[0].status = 'Suppressed';
                              reloadEvent(pendingEvent.id);
                            })
                        }
                      >
                        <Icon name="merge" /> Replace
                      </Button>
                    )}

                    <Button
                      className="btn-secondary"
                      onClick={() =>
                        EventsApi.patch(pendingEvent.id, {
                          status: 'Suppressed',
                        }).then(() => reloadEvent(pendingEvent.id))
                      }
                    >
                      <Icon name="archive" /> Archive / Reject
                    </Button>
                  </div>
                </div>
              </div>
            );
          })}
      </div>
    </section>
  );
};

export default ReviewPage;
