import React, { useCallback, useEffect, useState } from 'react';
import {
  Badge,
  Button,
  Card,
  Col,
  ListGroup,
  Row,
  Spinner,
  Tab,
  Table,
  Tabs,
} from 'react-bootstrap';

import cn from 'classnames';
import { ScrapersApi } from '../../api/scrapers';
import type {
  EventRevision,
  Guid,
  ScrapedEventRaw,
  ScraperAttemptHistory,
  ScraperDefinition,
} from '../../types/api';
import styles from './AttemptHistoryPage.module.css';

interface Loading {
  scrapers: boolean;
  attempts: boolean;
}

export const AttemptHistoryPage: React.FC = () => {
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);

  // State for Attempts List
  const [attempts, setAttempts] = useState<ScraperAttemptHistory[]>([]);
  const [scraperId, setScraperId] = useState<Guid | null>(null);
  const [nextCursor, setNextCursor] = useState<string | null>(null);

  // State for Selection & Details
  const [selectedAttempt, setSelectedAttempt] =
    useState<ScraperAttemptHistory | null>(null);
  const [selectedRow, setSelectedRow] = useState<string | null>(null);
  const [rawEvents, setRawEvents] = useState<ScrapedEventRaw[]>([]);
  const [creations, setCreations] = useState<ScrapedEventRaw[]>([]);
  const [revisions, setRevisions] = useState<EventRevision[]>([]);

  // UI State
  const [loading, setLoading] = useState<Loading>({
    scrapers: true,
    attempts: true,
  });
  const [detailLoading, setDetailLoading] = useState(false);

  useEffect(() => {
    var abortController = new AbortController();

    loadScrapers(abortController.signal);
    loadInitialAttempts(abortController.signal);
  }, [scraperId]);

  const loadScrapers = useCallback(async (signal?: AbortSignal) => {
    setLoading((prev) => ({ ...prev, scrapers: true }));
    try {
      const data = await ScrapersApi.getAll(signal);

      setScrapers(data);
    } catch (error) {
      console.error('Failed to fetch scrapers', error);
    } finally {
      setLoading((prev) => ({ ...prev, scrapers: false }));
    }
  }, []);

  const loadInitialAttempts = useCallback(async (signal?: AbortSignal) => {
    setLoading((prev) => ({ ...prev, attempts: true }));
    try {
      const response = await ScrapersApi.getPagedAttemptHistories(
        scraperId ?? undefined,
        15,
        undefined,
        signal,
      );

      setAttempts(response.items);
      setNextCursor(response.nextCursor);

      // Automatically select the most recent attempt
      if (response.items.length > 0) {
        handleSelectAttempt(response.items[0]);
      }
    } finally {
      setLoading((prev) => ({ ...prev, attempts: false }));
    }
  }, []);

  const loadMore = useCallback(async (signal?: AbortSignal) => {
    if (!nextCursor) return;

    setLoading((prev) => ({ ...prev, attempts: true }));
    try {
      const response = await ScrapersApi.getPagedAttemptHistories(
        scraperId ?? undefined,
        15,
        nextCursor,
        signal,
      );
      setAttempts((prev) => [...prev, ...response.items]);
      setNextCursor(response.nextCursor);
    } finally {
      setLoading((prev) => ({ ...prev, attempts: false }));
    }
  }, []);

  const handleSelectAttempt = async (attempt: ScraperAttemptHistory) => {
    setSelectedAttempt(attempt);
    setDetailLoading(true);

    try {
      // Fetch both Raw data and Revisions in parallel
      const [rawEventsResult, revisionsResult] = await Promise.all([
        ScrapersApi.getRawEventsByAttempt(attempt.id, 50),
        ScrapersApi.getRevisionsByAttempt(attempt.id),
      ]);

      const updateRawEventIds = revisionsResult.map((r) => r.rawEventId);

      setRawEvents(rawEventsResult.items);
      setCreations(
        rawEventsResult.items.filter(
          (r) => r.eventId && !updateRawEventIds.includes(r.id),
        ),
      );
      setRevisions(revisionsResult);
    } finally {
      setDetailLoading(false);
    }
  };

  const renderStatusBadge = (success: boolean) => (
    <Badge bg={success ? 'success' : 'danger'}>
      {success ? 'SUCCESS' : 'FAILED'}
    </Badge>
  );

  return (
    <div className="p-4">
      <h2 className="mb-4">Scraper Run History</h2>

      <Row>
        {/* LEFT COLUMN: HISTORY LIST */}
        <Col md={4}>
          <Card className="shadow-sm">
            <Card.Header className="font-weight-bold">
              Recent Attempts
            </Card.Header>
            <ListGroup
              variant="flush"
              style={{ maxHeight: 'calc(100vh - 220px)', overflowY: 'auto' }}
            >
              {attempts.map((a) => (
                <ListGroup.Item
                  key={a.id}
                  action
                  active={selectedAttempt?.id === a.id}
                  onClick={() => handleSelectAttempt(a)}
                  className="d-flex justify-content-between align-items-center"
                >
                  <div>
                    <em>{a.scraperName}</em>
                    <small className="d-block text-muted">
                      {new Date(a.startTimeUtc).toLocaleString()}
                    </small>
                    <strong>{a.extracted} items</strong>
                  </div>
                  {renderStatusBadge(a.success)}
                </ListGroup.Item>
              ))}
              {loading.attempts && (
                <div className="p-5 text-center">
                  <Spinner animation="border" />
                </div>
              )}
              {nextCursor && (
                <Button
                  variant="link"
                  size="sm"
                  className="text-center w-100 py-3"
                  onClick={() => loadMore()}
                >
                  Load Older...
                </Button>
              )}
            </ListGroup>
          </Card>
        </Col>

        {/* RIGHT COLUMN: ATTEMPT DETAILS */}
        <Col md={8}>
          {selectedAttempt ? (
            <>
              {/* FUNNEL STATS */}
              <Row className="mb-4">
                {[
                  {
                    label: 'Extracted',
                    val: selectedAttempt.extracted,
                    color: 'primary',
                  },
                  {
                    label: 'Skipped',
                    val: selectedAttempt.skipped,
                    color: 'info',
                  },
                  {
                    label: 'Created',
                    val: selectedAttempt.created,
                    color: 'success',
                  },
                  {
                    label: 'Updated',
                    val: selectedAttempt.updated,
                    color: 'warning',
                  },
                  {
                    label: 'Errors',
                    val: selectedAttempt.errored,
                    color: 'danger',
                  },
                ].map((stat, i) => (
                  <Col key={i}>
                    <Card className="text-center border-0 shadow-sm">
                      <Card.Body>
                        <h6 className="text-muted text-uppercase small">
                          {stat.label}
                        </h6>
                        <h3 className={`text-${stat.color}`}>{stat.val}</h3>
                      </Card.Body>
                    </Card>
                  </Col>
                ))}
              </Row>

              {selectedAttempt.errorMessage && (
                <Card border="danger" className="mb-4 bg-light-danger">
                  <Card.Body className="py-2 text-danger small font-monospace">
                    <strong>Error:</strong> {selectedAttempt.errorMessage}
                  </Card.Body>
                </Card>
              )}

              <Card className="shadow-sm">
                <Card.Body>
                  <Tabs
                    defaultActiveKey="revisions"
                    id="attempt-detail-tabs"
                    className="mb-3"
                  >
                    {/* TAB 0: CREATIONS */}
                    <Tab
                      eventKey="creations"
                      title={`Creations (${creations.length})`}
                    >
                      <Table responsive hover size="sm" className="small">
                        <thead>
                          <tr>
                            <th>Scraped Time</th>
                            <th>Raw JSON Snapshot</th>
                          </tr>
                        </thead>
                        <tbody>
                          {creations.map((raw) => (
                            <tr
                              key={raw.id}
                              onClick={() =>
                                setSelectedRow(
                                  raw.id == selectedRow ? null : raw.id,
                                )
                              }
                            >
                              <td className="text-nowrap">
                                {new Date(
                                  raw.scrapedAtUtc,
                                ).toLocaleTimeString()}
                              </td>
                              <td>
                                <pre
                                  className={cn(
                                    'border',
                                    'rounded',
                                    styles.Code,
                                    raw.id == selectedRow && styles.FullHeight,
                                  )}
                                >
                                  {JSON.stringify(
                                    JSON.parse(raw?.rawDataJson ?? '{}'),
                                    null,
                                    2,
                                  )}
                                </pre>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </Table>
                    </Tab>

                    {/* TAB 1: REVISIONS (THE DIFFS) */}
                    <Tab
                      eventKey="revisions"
                      title={`Changes (${revisions.length})`}
                    >
                      {detailLoading ? (
                        <Spinner animation="grow" size="sm" />
                      ) : (
                        <Table responsive hover size="sm" className="small">
                          <thead className="bg-light">
                            <tr>
                              <th>Event Title</th>
                              <th>Change Type</th>
                              <th>Field Diffs</th>
                            </tr>
                          </thead>
                          <tbody>
                            {revisions.map((rev) => (
                              <tr key={rev.id}>
                                <td>
                                  <strong>{rev.eventTitle || 'Unknown'}</strong>
                                </td>
                                <td>
                                  <Badge bg="info">{rev.reason}</Badge>
                                </td>
                                <td>
                                  <pre
                                    className="m-0 p-1 border rounded"
                                    style={{ fontSize: '10px' }}
                                  >
                                    {JSON.stringify(
                                      JSON.parse(
                                        rev?.changedFieldsJson ?? '{}',
                                      ),
                                      null,
                                      2,
                                    )}
                                  </pre>
                                </td>
                              </tr>
                            ))}
                            {revisions.length === 0 && (
                              <tr>
                                <td
                                  colSpan={3}
                                  className="text-center py-4 text-muted"
                                >
                                  No data changes in this run.
                                </td>
                              </tr>
                            )}
                          </tbody>
                        </Table>
                      )}
                    </Tab>

                    {/* TAB 2: RAW DATA (THE INCOMING JSON) */}
                    <Tab
                      eventKey="raw"
                      title={`Raw Scraped (${rawEvents.length})`}
                    >
                      <Table responsive hover size="sm" className="small">
                        <thead>
                          <tr>
                            <th>Scraped Time</th>
                            <th>Raw JSON Snapshot</th>
                          </tr>
                        </thead>
                        <tbody>
                          {rawEvents.map((raw) => (
                            <tr
                              key={raw.id}
                              onClick={() =>
                                setSelectedRow(
                                  raw.id == selectedRow ? null : raw.id,
                                )
                              }
                            >
                              <td className="text-nowrap">
                                {new Date(
                                  raw.scrapedAtUtc,
                                ).toLocaleTimeString()}
                              </td>
                              <td>
                                <pre
                                  className={cn(
                                    'border',
                                    'rounded',
                                    styles.Code,
                                    raw.id == selectedRow && styles.FullHeight,
                                  )}
                                >
                                  {JSON.stringify(
                                    JSON.parse(raw?.rawDataJson ?? '{}'),
                                    null,
                                    2,
                                  )}
                                </pre>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </Table>
                    </Tab>
                  </Tabs>
                </Card.Body>
              </Card>
            </>
          ) : (
            <div className="h-100 d-flex align-items-center justify-content-center text-muted">
              Select an attempt on the left to view details.
            </div>
          )}
        </Col>
      </Row>
    </div>
  );
};

export default AttemptHistoryPage;
