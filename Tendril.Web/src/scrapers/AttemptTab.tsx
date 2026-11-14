import { format, intervalToDuration, parseISO } from 'date-fns';
import {
  ArrowUpRight,
  CircleCheck,
  CircleX,
  FileText,
  FlaskConical,
  Play,
} from 'lucide-react';
import { Fragment, useEffect, useState } from 'react';
import { Badge, Card, Table } from 'react-bootstrap';
import { Link } from 'react-router';

import { ScrapersApi } from '../api/scrapers';
import { SquareButton as Button } from '../components/button';
import { ElapsedClock } from '../components/ElapsedClock';
import styles from './RunsTab.module.css';

import type {
  Guid,
  ScraperAttemptHistory,
  ScrapeRunResponse,
} from '../types/api';

interface Props {
  scraperId: Guid;
  attempts: ScraperAttemptHistory[];
  onComplete?: () => void;
}

export const RunsTab: React.FC<Props> = ({
  scraperId,
  attempts,
  onComplete,
}) => {
  const [result, setResult] = useState<ScrapeRunResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [runStart, setRunStart] = useState<Date | null>(null);
  const [clockNow, setClockNow] = useState(Date.now());

  useEffect(() => {
    const timerId = window.setInterval(() => setClockNow(Date.now()), 1000);
    return () => window.clearInterval(timerId);
  }, []);

  const run = async (kind: 'actions' | 'mapping' | 'test' | 'now') => {
    setRunStart(new Date());
    setLoading(true);
    setResult(null);
    try {
      let res: ScrapeRunResponse;
      switch (kind) {
        case 'actions':
          res = await ScrapersApi.testActions(scraperId);
          break;
        case 'mapping':
          res = await ScrapersApi.testMapping(scraperId);
          break;
        case 'test':
          res = await ScrapersApi.testRun(scraperId);
          break;
        case 'now':
        default:
          res = await ScrapersApi.runNow(scraperId);
          break;
      }
      setResult(res);
    } catch (e: any) {
      setResult({
        success: false,
        error: e.message ?? 'Error running scraper.',
      });
    } finally {
      onComplete?.();
      setLoading(false);
    }
  };

  const [selectedAttemptId, setSelectedAttemptId] = useState<Guid | null>(null);

  const selectedAttempt = attempts.find((a) => a.id == selectedAttemptId);

  return (
    <div className={styles.RunsPage}>
      <section className={styles.ToolSection}>
        <div className={styles.SectionHeading}>
          <div>
            <p className={styles.Eyebrow}>SCRAPER OPERATIONS</p>
            <h2>Test tools</h2>
            <p>
              Run a focused check or start a full ingestion for this scraper.
            </p>
          </div>
          {loading && <ElapsedClock runStart={runStart} />}
        </div>
        <div className={styles.ButtonContainer}>
          <Button
            disabled={loading}
            variant="outline-secondary"
            onClick={() => run('actions')}
          >
            <FlaskConical size={16} /> Test actions
          </Button>
          <Button
            disabled={loading}
            variant="outline-secondary"
            onClick={() => run('test')}
          >
            <FileText size={16} /> Test actions + mapping
          </Button>
          <Button
            disabled={loading}
            variant="primary"
            onClick={() => run('now')}
          >
            <Play size={16} /> Run now
          </Button>
        </div>

        {result && (
          <Card
            className={`${styles.Result} ${result.success ? styles.ResultSuccess : styles.ResultFailure}`}
          >
            <Card.Body>
              <div className={styles.ResultHeading}>
                {result.success ? (
                  <CircleCheck size={18} />
                ) : (
                  <CircleX size={18} />
                )}
                <strong>
                  {result.success ? 'Run completed successfully' : 'Run failed'}
                </strong>
              </div>
              {result.error && (
                <p className={styles.ResultError}>{result.error}</p>
              )}
              <div className={styles.ResultPayloads}>
                <details>
                  <summary>Raw result ({result.raw?.length ?? 0})</summary>
                  <pre>{JSON.stringify(result.raw, null, 2)}</pre>
                </details>
                <details>
                  <summary>
                    Mapped result ({result.mapped?.length ?? 0})
                  </summary>
                  <pre>{JSON.stringify(result.mapped, null, 2)}</pre>
                </details>
                <details>
                  <summary>Logs ({result.logs?.length ?? 0})</summary>
                  <pre>{JSON.stringify(result.logs, null, 2)}</pre>
                </details>
              </div>
            </Card.Body>
          </Card>
        )}
      </section>

      <section className={styles.RunSection}>
        <div className={styles.SectionHeading}>
          <div>
            <p className={styles.Eyebrow}>EXECUTION HISTORY</p>
            <h2>
              Runs <span className={styles.Count}>{attempts.length}</span>
            </h2>
            <p>
              Recent attempts for this scraper, whether scheduled or started
              manually.
            </p>
          </div>
        </div>
        <div className={styles.TableWrap}>
          <Table hover responsive className={styles.RunTable}>
            <thead>
              <tr>
                <th>Started</th>
                <th>Duration</th>
                <th>Origin</th>
                <th>Outcome</th>
                <th>Extracted</th>
                <th>Mapped</th>
                <th>Created</th>
                <th>Updated</th>
                <th>Skipped</th>
                <th>Errors</th>
                <th>Message</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {attempts.map((attempt) => {
                const isRunning = attempt.errorMessage === 'Running...';
                return (
                  <Fragment key={attempt.id}>
                    <tr className={isRunning ? styles.ActiveRow : undefined}>
                      <td className={styles.StartCell}>
                        <strong>
                          {format(
                            parseISO(attempt.startTimeUtc),
                            'MMM d, yyyy',
                          )}
                        </strong>
                        <span>
                          {format(parseISO(attempt.startTimeUtc), 'h:mm:ss a')}
                        </span>
                      </td>
                      <td className={styles.DurationCell}>
                        {isRunning ? (
                          <ElapsedDuration
                            startedAt={attempt.startTimeUtc}
                            now={clockNow}
                          />
                        ) : (
                          formatRunDuration(
                            attempt.startTimeUtc,
                            attempt.endTimeUtc,
                          )
                        )}
                      </td>
                      <td>
                        <Badge
                          bg={
                            attempt.scheduledTaskRunId ? 'primary' : 'secondary'
                          }
                          className={styles.Badge}
                        >
                          {attempt.scheduledTaskRunId ? 'Scheduled' : 'Manual'}
                        </Badge>
                      </td>
                      <td>
                        <Badge
                          bg={
                            isRunning
                              ? 'warning'
                              : attempt.success
                                ? 'success'
                                : 'danger'
                          }
                          text={isRunning ? 'dark' : undefined}
                          className={styles.Badge}
                        >
                          {isRunning
                            ? 'Running'
                            : attempt.success
                              ? 'Succeeded'
                              : 'Failed'}
                        </Badge>
                      </td>
                      <td>{attempt.extracted}</td>
                      <td>{attempt.mapped}</td>
                      <td>{attempt.created}</td>
                      <td>{attempt.updated}</td>
                      <td>{attempt.skipped}</td>
                      <td>{attempt.errored}</td>
                      <td
                        className={styles.ErrorMessage}
                        title={attempt.errorMessage ?? undefined}
                      >
                        {isRunning
                          ? 'Currently in progress'
                          : attempt.errorMessage || '—'}
                      </td>
                      <td className={styles.Actions}>
                        {attempt.logEntries.length > 0 && (
                          <Button
                            variant="outline-secondary"
                            size="sm"
                            onClick={() =>
                              setSelectedAttemptId(
                                selectedAttemptId === attempt.id
                                  ? null
                                  : attempt.id,
                              )
                            }
                          >
                            {selectedAttemptId === attempt.id
                              ? 'Hide logs'
                              : 'Logs'}
                          </Button>
                        )}
                        <Link
                          to={`/admin/scrapers/${scraperId}/runs/${attempt.id}`}
                          className={styles.OpenLink}
                        >
                          Open <ArrowUpRight size={14} />
                        </Link>
                      </td>
                    </tr>
                    {selectedAttemptId === attempt.id &&
                      selectedAttempt?.logEntries.map((entry, index) => (
                        <tr
                          key={`${entry.timestampUtc}-${index}`}
                          className={styles.LogRow}
                        >
                          <td colSpan={12}>
                            <span>
                              {format(
                                parseISO(entry.timestampUtc),
                                'h:mm:ss a',
                              )}
                            </span>
                            <Badge bg="light" text="dark">
                              {entry.level}
                            </Badge>
                            <code>{entry.message}</code>
                          </td>
                        </tr>
                      ))}
                  </Fragment>
                );
              })}
              {!attempts.length && (
                <tr>
                  <td colSpan={12} className={styles.Empty}>
                    No attempts recorded for this scraper.
                  </td>
                </tr>
              )}
            </tbody>
          </Table>
        </div>
      </section>
    </div>
  );
};

function ElapsedDuration({
  startedAt,
  now,
}: {
  startedAt: string;
  now: number;
}) {
  const seconds = Math.max(
    0,
    Math.floor((now - new Date(startedAt).getTime()) / 1000),
  );
  if (!Number.isFinite(seconds)) return <>—</>;
  if (seconds < 60) return <>{seconds}s</>;
  if (seconds < 3600)
    return (
      <>
        {Math.floor(seconds / 60)}m {seconds % 60}s
      </>
    );
  return (
    <>
      {Math.floor(seconds / 3600)}h {Math.floor((seconds % 3600) / 60)}m
    </>
  );
}

function formatRunDuration(startedAt: string, endedAt: string | null): string {
  if (!endedAt) return '—';
  const duration = intervalToDuration({
    start: parseISO(startedAt),
    end: parseISO(endedAt),
  });
  const parts = [
    duration.days ? `${duration.days}d` : null,
    duration.hours ? `${duration.hours}h` : null,
    duration.minutes ? `${duration.minutes}m` : null,
    `${duration.seconds ?? 0}s`,
  ].filter(Boolean);
  return parts.join(' ');
}
