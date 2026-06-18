import cn from 'classnames';
import React, { useCallback, useEffect, useState } from 'react';
import { Badge, Col, Row, Spinner } from 'react-bootstrap';
import { CategoriesApi } from '../../api/categories';
import { EventsApi } from '../../api/events';
import { SquareButton as Button } from '../../components/button';
import { FormSelect } from '../../components/form';
import { Icon } from '../../components/Icon';
import { ReviewEventCard } from '../../events';
import { pageStyles } from '../../styles';
import type { Category, PendingEventReviewDto } from '../../types/api';
import styles from './ReviewPage.module.css';

export const ReviewPage: React.FC = () => {
  // Use the updated TypeScript interface name representing the new C# DTO wrapper
  const [reviewPayloads, setReviewPayloads] = useState<PendingEventReviewDto[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    try {
      // Calls your updated C# [HttpGet("pending-review")]
      const [reviewData, categoriesData] = await Promise.all([
        EventsApi.getPending(signal), 
        CategoriesApi.getAll(signal)
      ]);
      setReviewPayloads(reviewData);
      setCategories(categoriesData.sort((a, b) => a.name.localeCompare(b.name)));
    } catch (e) {
      console.error('Failed to sync review portal', e);
    } finally {
      setLoading(false);
    }
  }, []);

  // const reloadEvent = useCallback(async (eventId: Guid, signal?: AbortSignal) => {
  //   try {
  //     const updated = EventsApi
  //   }
  // })

  useEffect(() => {
    const controller = new AbortController();
    loadData(controller.signal);
    return () => controller.abort();
  }, [loadData]);

  if (loading) {
    return <div className="text-center my-5"><Spinner animation="border" variant="light" /></div>;
  }

  return (
    <section className="container-fluid py-4">
      <div className={pageStyles.pageHeader}>
        <h2>Pending Review Pipeline</h2>
        <p className="text-muted">Directly comparing fresh scraper batches against live system states.</p>
      </div>

      <div className="d-flex flex-column gap-4">
        {/* Map through the parent wrapper elements returned by C# */}
        {reviewPayloads.map(({ pendingEvent, potentialMatches }) => {
          const hasConflict = potentialMatches && potentialMatches.length > 0;

          return (
            <div 
              key={pendingEvent.id} 
              className={cn(styles.reviewRowGroup, { [styles.conflictDetected]: hasConflict })}
            >
              {hasConflict && (
                <div className={styles.conflictHeader}>
                  <Icon name="warning" /> Conflict Identified: This looks like an update to an existing live event
                </div>
              )}

              <Row className="g-3 align-items-stretch">
                {/* Left Side: The Raw Incoming Scraped Event */}
                <Col lg={hasConflict ? 6 : 12}>
                  <div className="position-relative h-100">
                    <Badge bg="warning" text="dark" className={styles.statusBadge}>
                      INCOMING PENDING (From: {/*pendingEvent.scraperName ??*/ 'Unknown Scraper'})
                    </Badge>
                    <ReviewEventCard e={pendingEvent} />
                  </div>
                </Col>

                {/* Right Side: The Existing Published Candidate Event(s) */}
                {hasConflict && (
                  <Col lg={6}>
                    <div className="d-flex flex-column gap-2 h-100 justify-content-center">
                      {potentialMatches.map((existingEvent) => (
                        <div key={existingEvent.id} className="position-relative opacity-75">
                          <Badge bg="success" className={styles.statusBadge}>
                            LIVE PUBLISHED (ID: {existingEvent.id.substring(0, 5)})
                          </Badge>
                          <ReviewEventCard e={existingEvent} />
                        </div>
                      ))}
                    </div>
                  </Col>
                )}
              </Row>

              {/* Universal Action Toolbar positioned cleanly underneath the cards */}
              <div className="d-flex justify-content-between align-items-center mt-3 pt-2 border-top border-secondary">
                <div style={{ width: '200px' }}>
                  <FormSelect
                    value={categories?.find((c) => c.name === pendingEvent.categoryName)?.id ?? ''}
                    options={[
                      { value: '', label: 'Select Category' },
                      ...categories.map((c) => ({ value: c.id, label: c.name })),
                    ]}
                    onChange={(val) => EventsApi.patch(pendingEvent.id, { categoryId: val }).then(() => loadData())}
                  />
                </div>

                <div className="d-flex gap-2">
                  <Button 
                    className="btn-success"
                    onClick={() => EventsApi.patch(pendingEvent.id, { status: 'Published' })}
                  >
                    <Icon name="publish" /> Approve & Publish New
                  </Button>
                  
                  {hasConflict && (
                    <Button 
                      className="btn-info"
                      disabled
                      // onClick={() => EventsApi.mergeAndPublish(pendingEvent.id, potentialMatches[0].id).then(() => loadData())}
                    >
                      <Icon name="merge" /> Merge Updates to Live
                    </Button>
                  )}

                  <Button 
                    className="btn-secondary"
                    onClick={() => EventsApi.patch(pendingEvent.id, { status: 'Suppressed' }).then(() => loadData())}
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