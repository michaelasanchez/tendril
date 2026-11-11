import { ArrowUpRight, ChevronDown, ChevronRight, Clock3, RefreshCw } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Alert, Badge, Button, Spinner, Table } from 'react-bootstrap';
import { Link } from 'react-router';

import { ScheduledTaskRunsApi } from '../../api/scheduledTaskRuns';
import styles from './TaskRunsPage.module.css';

import type { ScheduledTaskRun } from '../../types/api';
const RUN_LIMIT = 100;

export const TaskRunsPage: React.FC = () => {
  const [runs, setRuns] = useState<ScheduledTaskRun[]>([]);
  const [expandedRunIds, setExpandedRunIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [refreshToken, setRefreshToken] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    const timerId = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timerId);
  }, []);

  useEffect(() => {
    let active = true;
    let inFlight = false;

    const loadRuns = async () => {
      if (inFlight) return;
      inFlight = true;
      setRefreshing(true);

      try {
        const result = await ScheduledTaskRunsApi.getRecent(RUN_LIMIT);
        if (!active) return;

        setRuns(result);
        setExpandedRunIds((current) => {
          const next = new Set(current);
          result
            .filter(
              (run) => run.status === 'Running' || run.status === 'Retrying',
            )
            .forEach((run) => next.add(run.id));
          return next;
        });
        setError(null);
        setLastUpdated(new Date());
      } catch (requestError) {
        if (active) {
          setError(
            requestError instanceof Error
              ? requestError.message
              : 'Could not load scheduled task runs.',
          );
        }
      } finally {
        inFlight = false;
        if (active) {
          setLoading(false);
          setRefreshing(false);
        }
      }
    };

    void loadRuns();
    const intervalId = window.setInterval(() => void loadRuns(), 30_000);

    return () => {
      active = false;
      window.clearInterval(intervalId);
    };
  }, [refreshToken]);

  const activeRuns = runs.filter(
    (run) => run.status === 'Running' || run.status === 'Retrying',
  ).length;
  const failedRuns = runs.filter((run) => run.status === 'Failed').length;
  const completedRuns = runs.filter((run) => run.status === 'Completed').length;
  const attemptTotal = runs.reduce((total, run) => total + run.attemptCount, 0);

  const toggleRun = (runId: string) => {
    setExpandedRunIds((current) => {
      const next = new Set(current);
      if (next.has(runId)) next.delete(runId);
      else next.add(runId);
      return next;
    });
  };

  return (
    <main className={styles.Page}>
      <header className={styles.PageHeader}>
        <div>
          <p className={styles.Eyebrow}>AUTOMATION HISTORY</p>
          <h1>Scheduled task runs</h1>
          <p className={styles.Subtitle}>
            Each run groups the scraper attempts performed for one scheduled
            task.
          </p>
        </div>
        <div className={styles.HeaderActions}>
          <span className={styles.UpdatedAt}>
            {lastUpdated
              ? `Updated ${formatClock(lastUpdated)}`
              : 'Waiting for first refresh'}
          </span>
          <Button
            variant="outline-secondary"
            size="sm"
            disabled={refreshing}
            onClick={() => setRefreshToken((value) => value + 1)}
          >
            <RefreshCw
              size={15}
              className={refreshing ? styles.Spin : undefined}
            />
            Refresh
          </Button>
        </div>
      </header>

      {error && <Alert variant="warning">{error}</Alert>}

      <section className={styles.Summary} aria-label="Recent run summary">
        <SummaryValue label="Runs shown" value={runs.length} />
        <SummaryValue label="In progress / retrying" value={activeRuns} />
        <SummaryValue label="Completed" value={completedRuns} />
        <SummaryValue label="Failed" value={failedRuns} />
        <SummaryValue label="Scraper attempts" value={attemptTotal} />
      </section>

      <section className={styles.RunSection}>
        <div className={styles.SectionHeading}>
          <h2>Latest runs</h2>
          <span>Most recent {RUN_LIMIT}</span>
        </div>
        <div className={styles.TableWrap}>
          <Table responsive className={styles.RunTable}>
            <thead>
              <tr>
                <th>Scheduled task</th>
                <th>Outcome</th>
                <th>Started</th>
                <th>Duration</th>
                <th>Retries</th>
                <th>Attempt results</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {runs.map((run) => {
                const expanded = expandedRunIds.has(run.id);
                const active =
                  run.status === 'Running' || run.status === 'Retrying';
                return (
                  <RunRows
                    key={run.id}
                    run={run}
                    now={now}
                    active={active}
                    expanded={expanded}
                    onToggle={() => toggleRun(run.id)}
                  />
                );
              })}
              {!runs.length && !loading && (
                <tr>
                  <td colSpan={7} className={styles.Empty}>
                    No scheduled task runs recorded.
                  </td>
                </tr>
              )}
              {loading && (
                <tr>
                  <td colSpan={7} className={styles.Loading}>
                    <Spinner size="sm" /> Loading scheduled task runs...
                  </td>
                </tr>
              )}
            </tbody>
          </Table>
        </div>
        <p className={styles.Footnote}>
          Showing up to {RUN_LIMIT} recent runs. Manual attempts are listed
          separately in <Link to="/admin/summary">Attempt History</Link>.
        </p>
      </section>
    </main>
  );
};

function RunRows({
  run,
  now,
  active,
  expanded,
  onToggle,
}: {
  run: ScheduledTaskRun;
  now: number;
  active: boolean;
  expanded: boolean;
  onToggle: () => void;
}) {
  return (
    <>
      <tr className={active ? styles.ActiveRun : undefined}>
        <td className={styles.TaskCell}>
          <strong>{run.scheduledTaskName}</strong>
          <small>{run.id}</small>
        </td>
        <td>
          <RunStatus status={run.status} />
        </td>
        <td>{formatDateTime(run.startTimeUtc)}</td>
        <td className={active ? styles.ActiveDuration : styles.DurationCell}>
          {active && <Clock3 size={13} />}
          {formatDuration(run.startTimeUtc, run.endTimeUtc, now)}
        </td>
        <td>{run.retryCount}</td>
        <td>
          <div className={styles.AttemptCounts}>
            <span className={styles.SuccessCount}>
              {run.successfulAttemptCount} succeeded
            </span>
            <span className={styles.FailureCount}>
              {run.failedAttemptCount} failed
            </span>
            {run.inProgressAttemptCount > 0 && (
              <span className={styles.ActiveCount}>
                {run.inProgressAttemptCount} running
              </span>
            )}
          </div>
        </td>
        <td>
          <Button
            variant="link"
            size="sm"
            className={styles.ExpandButton}
            onClick={onToggle}
            aria-expanded={expanded}
          >
            {expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
            {expanded ? 'Hide' : 'View'} {run.attemptCount} attempts
          </Button>
        </td>
      </tr>
      {expanded && (
        <tr className={styles.AttemptGroupRow}>
          <td colSpan={7}>
            <div className={styles.AttemptGroup}>
              <div className={styles.GroupHeading}>
                <strong>Attempts in this run</strong>
                <span>Run started {formatDateTime(run.startTimeUtc)}</span>
              </div>
              {run.attempts.length > 0 ? (
                <div className={styles.AttemptTableWrap}>
                  <Table responsive size="sm" className={styles.AttemptTable}>
                    <thead>
                      <tr>
                        <th>Scraper</th>
                        <th>Result</th>
                        <th>Started</th>
                        <th>Duration</th>
                        <th>Events</th>
                        <th>Issue</th>
                        <th />
                      </tr>
                    </thead>
                    <tbody>
                      {run.attempts.map((attempt) => {
                        const attemptActive = attempt.status === 'Running';
                        return (
                          <tr key={attempt.id}>
                            <td className={styles.ScraperCell}>
                              {attempt.scraperName}
                            </td>
                            <td>
                              <AttemptStatus status={attempt.status} />
                            </td>
                            <td>{formatDateTime(attempt.startTimeUtc)}</td>
                            <td
                              className={
                                attemptActive
                                  ? styles.ActiveDuration
                                  : styles.DurationCell
                              }
                            >
                              {attemptActive && <Clock3 size={13} />}
                              {formatDuration(
                                attempt.startTimeUtc,
                                attempt.endTimeUtc,
                                now,
                              )}
                            </td>

                            <td>
                              {attempt.extracted} extracted · {attempt.created}{' '}
                              created · {attempt.updated} updated
                            </td>

                            <td
                              className={styles.IssueCell}
                              title={attempt.errorMessage ?? undefined}
                            >
                              {attempt.errorMessage || '—'}
                            </td>
                            <td>
                              <Link
                                className={styles.OpenLink}
                                to={`/admin/scrapers/${attempt.scraperDefinitionId}/runs/${attempt.id}`}
                              >
                                Attempt <ArrowUpRight size={14} />
                              </Link>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </Table>
                </div>
              ) : (
                <p className={styles.NoAttempts}>
                  No scraper attempts recorded for this run.
                </p>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

function SummaryValue({ label, value }: { label: string; value: number }) {
  return (
    <div className={styles.SummaryValue}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function RunStatus({ status }: { status: string }) {
  const variant =
    status === 'Completed'
      ? 'success'
      : status === 'Failed'
        ? 'danger'
        : status === 'Retrying'
          ? 'warning'
          : 'primary';
  return (
    <Badge
      bg={variant}
      text={variant === 'warning' ? 'dark' : undefined}
      className={styles.StatusBadge}
    >
      {status}
    </Badge>
  );
}

function AttemptStatus({ status }: { status: string }) {
  const variant =
    status === 'Succeeded'
      ? 'success'
      : status === 'Failed'
        ? 'danger'
        : 'warning';
  return (
    <Badge
      bg={variant}
      text={variant === 'warning' ? 'dark' : undefined}
      className={styles.StatusBadge}
    >
      {status}
    </Badge>
  );
}

function formatDateTime(value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString();
}

function formatClock(date: Date): string {
  return date.toLocaleTimeString([], {
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
  });
}

function formatDuration(
  start: string,
  end: string | null,
  now: number,
): string {
  const startTime = new Date(start).getTime();
  const endTime = end ? new Date(end).getTime() : now;
  const seconds = Math.max(0, Math.floor((endTime - startTime) / 1000));
  if (!Number.isFinite(seconds)) return '—';
  if (seconds < 60) return `${seconds}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`;
  if (seconds < 86400)
    return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
  return `${Math.floor(seconds / 86400)}d ${Math.floor((seconds % 86400) / 3600)}h`;
}

export default TaskRunsPage;
