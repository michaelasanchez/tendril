import { useEffect, useState } from 'react';
import { Alert, Badge, Col, Row, Spinner, Tab, Table, Tabs } from 'react-bootstrap';
import { Link, useParams } from 'react-router';
import { ScrapersApi } from '../api/scrapers';
import type { EventRevision, PagedResponse, ScrapedEventRaw, ScraperAttemptHistory } from '../types/api';

export const AttemptDetailPage: React.FC = () => {
  const { scraperId, runId } = useParams();
  const [attempt, setAttempt] = useState<ScraperAttemptHistory | null>(null);
  const [rawEvents, setRawEvents] = useState<PagedResponse<ScrapedEventRaw> | null>(null);
  const [revisions, setRevisions] = useState<EventRevision[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [clockNow, setClockNow] = useState(Date.now());

  useEffect(() => {
    const timerId = window.setInterval(() => setClockNow(Date.now()), 1000);
    return () => window.clearInterval(timerId);
  }, []);

  useEffect(() => {
    if (!scraperId || !runId) {
      setError('The attempt URL is incomplete.');
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    setLoading(true);
    setError(null);

    void (async () => {
      try {
        const result = await ScrapersApi.getAttemptById(runId, controller.signal);
        if (result.scraperDefinitionId !== scraperId) {
          setError('This attempt does not belong to the requested scraper.');
          setAttempt(null);
          return;
        }

        setAttempt(result);
        const [rawResult, revisionResult] = await Promise.all([
          ScrapersApi.getRawEventsByAttempt(runId, 100, undefined, controller.signal),
          ScrapersApi.getRevisionsByAttempt(runId, controller.signal),
        ]);
        if (controller.signal.aborted) return;
        setRawEvents(rawResult);
        setRevisions(revisionResult);
      } catch (requestError) {
        if (!controller.signal.aborted) {
          setError(requestError instanceof Error ? requestError.message : 'Could not load this attempt.');
        }
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    })();

    return () => controller.abort();
  }, [scraperId, runId]);

  const isRunning = attempt?.errorMessage === 'Running...';
  const originLabel = attempt?.scheduledTaskRunId ? 'Scheduled task run' : 'Manual run';

  return (
    <main className="py-4">
      <p className="mb-2"><Link to={`/admin/scrapers/${scraperId}/runs`}>← Scraper runs</Link></p>
      <header className="d-flex flex-wrap align-items-start justify-content-between gap-3 mb-4">
        <div>
          <h1 className="h3 mb-2">{attempt?.scraperName ?? 'Scraper attempt'}</h1>
          <div className="d-flex flex-wrap align-items-center gap-2 text-muted">
            <span>{attempt ? new Date(attempt.startTimeUtc).toLocaleString() : 'Loading attempt details'}</span>
            {attempt && <Badge bg={attempt.scheduledTaskRunId ? 'primary' : 'secondary'}>{originLabel}</Badge>}
            {isRunning && <Badge bg="warning" text="dark">Running</Badge>}
            {attempt && !isRunning && <Badge bg={attempt.success ? 'success' : 'danger'}>{attempt.success ? 'Succeeded' : 'Failed'}</Badge>}
          </div>
        </div>
        {attempt?.scheduledTaskRunId && (
          <span className="text-muted small">Task run {attempt.scheduledTaskRunId}</span>
        )}
      </header>

      {error && <Alert variant="danger">{error}</Alert>}
      {loading && <div className="py-5 text-center"><Spinner /></div>}

      {attempt && !loading && (
        <>
          <Row className="g-3 mb-4">
            {[
              ['Extracted', attempt.extracted],
              ['Mapped', attempt.mapped],
              ['Created', attempt.created],
              ['Updated', attempt.updated],
              ['Skipped', attempt.skipped],
              ['Errors', attempt.errored],
            ].map(([label, value]) => (
              <Col xs={6} md={4} lg={2} key={label}>
                <div className="border rounded p-3 h-100">
                  <div className="small text-muted text-uppercase">{label}</div>
                  <strong className="fs-4">{value}</strong>
                </div>
              </Col>
            ))}
          </Row>

          <dl className="row border-top border-bottom py-3 mb-4">
            <dt className="col-sm-3">Started</dt>
            <dd className="col-sm-9">{new Date(attempt.startTimeUtc).toLocaleString()}</dd>
            <dt className="col-sm-3">Ended</dt>
            <dd className="col-sm-9">{attempt.endTimeUtc ? new Date(attempt.endTimeUtc).toLocaleString() : 'Still running'}</dd>
            <dt className="col-sm-3">Duration</dt>
            <dd className="col-sm-9">{formatDuration(attempt.startTimeUtc, attempt.endTimeUtc, clockNow)}</dd>
          </dl>

          {attempt.errorMessage && !isRunning && <Alert variant={attempt.success ? 'info' : 'danger'}>{attempt.errorMessage}</Alert>}

          <Tabs defaultActiveKey="logs" className="mb-3">
            <Tab eventKey="logs" title={`Logs (${attempt.logEntries.length})`}>
              <Table responsive hover size="sm">
                <thead><tr><th>Time</th><th>Level</th><th>Message</th></tr></thead>
                <tbody>
                  {attempt.logEntries.map((entry, index) => (
                    <tr key={`${entry.timestampUtc}-${index}`}>
                      <td className="text-nowrap">{new Date(entry.timestampUtc).toLocaleTimeString()}</td>
                      <td>{entry.level}</td>
                      <td className="text-break">{entry.message}</td>
                    </tr>
                  ))}
                  {!attempt.logEntries.length && <tr><td colSpan={3} className="text-center text-muted py-4">No log entries.</td></tr>}
                </tbody>
              </Table>
            </Tab>
            <Tab eventKey="raw" title={`Raw captures (${rawEvents?.totalCount ?? 0})`}>
              <Table responsive hover size="sm">
                <thead><tr><th>Captured</th><th>Event</th><th>Raw data</th></tr></thead>
                <tbody>
                  {(rawEvents?.items ?? []).map((raw) => (
                    <tr key={raw.id}>
                      <td className="text-nowrap">{new Date(raw.scrapedAtUtc).toLocaleString()}</td>
                      <td>{raw.eventId ?? 'Unmapped'}</td>
                      <td><pre className="mb-0" style={{ maxWidth: 700, maxHeight: 220, overflow: 'auto' }}>{formatJson(raw.rawDataJson)}</pre></td>
                    </tr>
                  ))}
                  {!rawEvents?.items.length && <tr><td colSpan={3} className="text-center text-muted py-4">No raw captures.</td></tr>}
                </tbody>
              </Table>
              {(rawEvents?.totalCount ?? 0) > (rawEvents?.items.length ?? 0) && (
                <p className="small text-muted">Showing the first {rawEvents?.items.length} of {rawEvents?.totalCount} captures.</p>
              )}
            </Tab>
            <Tab eventKey="changes" title={`Event changes (${revisions.length})`}>
              <Table responsive hover size="sm">
                <thead><tr><th>Time</th><th>Event</th><th>Change</th><th>Fields</th></tr></thead>
                <tbody>
                  {revisions.map((revision) => (
                    <tr key={revision.id}>
                      <td className="text-nowrap">{new Date(revision.changedAtUtc).toLocaleString()}</td>
                      <td>{revision.eventTitle ?? revision.eventId}</td>
                      <td>{revision.reason}</td>
                      <td><pre className="mb-0">{formatJson(revision.changedFieldsJson)}</pre></td>
                    </tr>
                  ))}
                  {!revisions.length && <tr><td colSpan={4} className="text-center text-muted py-4">No event changes.</td></tr>}
                </tbody>
              </Table>
            </Tab>
          </Tabs>
        </>
      )}
    </main>
  );
};

function formatDuration(start: string, end: string | null | undefined, now: number): string {
  const startTime = new Date(start).getTime();
  const endTime = end ? new Date(end).getTime() : now;
  const seconds = Math.max(0, Math.floor((endTime - startTime) / 1000));
  if (seconds < 60) return `${seconds}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
}

function formatJson(value: string | null): string {
  if (!value) return '';
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

export default AttemptDetailPage;
