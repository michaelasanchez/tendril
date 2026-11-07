import { useEffect, useState, type ReactNode } from 'react';
import { Activity, ArrowUpRight, CalendarClock, CircleCheck, CircleX, Clock3, RefreshCw } from 'lucide-react';
import { Alert, Badge, Button, Spinner, Table } from 'react-bootstrap';
import { Link } from 'react-router';
import { ScheduledTaskRunsApi } from '../../api/scheduledTaskRuns';
import { ScheduledTasksApi } from '../../api/scheduledTasks';
import { WorkerApi, type WorkerStatus } from '../../api/worker';
import { ScrapersApi } from '../../api/scrapers';
import type { PagedResponse, ScheduledTask, ScheduledTaskRun, ScraperAttemptHistory } from '../../types/api';
import styles from './WorkerPage.module.css';

const ATTEMPT_SAMPLE_SIZE = 30;

export const WorkerPage: React.FC = () => {
  const [worker, setWorker] = useState<WorkerStatus | null>(null);
  const [tasks, setTasks] = useState<ScheduledTask[]>([]);
  const [runs, setRuns] = useState<ScheduledTaskRun[]>([]);
  const [attemptPage, setAttemptPage] = useState<PagedResponse<ScraperAttemptHistory> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hasLoaded, setHasLoaded] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [clockNow, setClockNow] = useState(Date.now());

  useEffect(() => {
    const timerId = window.setInterval(() => setClockNow(Date.now()), 1000);
    return () => window.clearInterval(timerId);
  }, []);

  useEffect(() => {
    let active = true;
    let inFlight = false;

    const load = async () => {
      if (inFlight) return;
      inFlight = true;
      setRefreshing(true);

      try {
        const results = await Promise.allSettled([
          WorkerApi.getStatus(),
          ScheduledTasksApi.getAll(),
          ScheduledTaskRunsApi.getRecent(20),
          ScrapersApi.getPagedAttemptHistories(undefined, ATTEMPT_SAMPLE_SIZE),
        ] as const);

        if (!active) return;

        const failures: string[] = [];
        const [workerResult, taskResult, runResult, attemptResult] = results;

        if (workerResult.status === 'fulfilled') setWorker(workerResult.value);
        else failures.push('worker status');

        if (taskResult.status === 'fulfilled') setTasks(taskResult.value);
        else failures.push('schedules');

        if (runResult.status === 'fulfilled') setRuns(runResult.value);
        else failures.push('task runs');

        if (attemptResult.status === 'fulfilled') setAttemptPage(attemptResult.value);
        else failures.push('attempt history');

        setError(failures.length ? `Could not refresh ${failures.join(', ')}.` : null);
        setLastUpdated(new Date());
      } finally {
        inFlight = false;
        if (active) {
          setHasLoaded(true);
          setRefreshing(false);
        }
      }
    };

    void load();
    const intervalId = window.setInterval(() => void load(), 30_000);

    return () => {
      active = false;
      window.clearInterval(intervalId);
    };
  }, [refreshKey]);

  const attempts = attemptPage?.items ?? [];
  const completedAttempts = attempts.filter((attempt) => attempt.errorMessage !== 'Running...');
  const successfulAttempts = completedAttempts.filter((attempt) => attempt.success).length;
  const failedAttempts = completedAttempts.length - successfulAttempts;
  const successRate = completedAttempts.length
    ? Math.round((successfulAttempts / completedAttempts.length) * 100)
    : null;

  const enabledTasks = tasks.filter((task) => !task.isDisabled);
  const runningTasks = enabledTasks.filter((task) => task.status === 'Running');
  const retryingTasks = enabledTasks.filter((task) => task.status === 'Retrying');
  const scheduledTasks = [...enabledTasks]
    .sort((a, b) => new Date(a.nextRunAtUtc).getTime() - new Date(b.nextRunAtUtc).getTime())
    .slice(0, 8);
  const activeTask = runningTasks[0];

  const statusClass = !worker || worker.status === 'Unknown'
    ? styles.Unknown
    : worker.isOnline
      ? styles.Online
      : worker.status === 'Stopped'
        ? styles.Stopped
        : styles.Offline;

  return (
    <main className={styles.Page}>
      <header className={styles.PageHeader}>
        <div>
          <p className={styles.Eyebrow}>OPERATIONS</p>
          <h1>Worker overview</h1>
          <p className={styles.Subtitle}>Process health, schedule queue, and recent execution outcomes.</p>
        </div>
        <div className={styles.HeaderActions}>
          <span className={styles.UpdatedAt}>
            {lastUpdated ? `Updated ${formatClock(lastUpdated)}` : 'Waiting for first refresh'}
          </span>
          <Button variant="outline-secondary" size="sm" onClick={() => setRefreshKey((key) => key + 1)} disabled={refreshing}>
            <RefreshCw size={15} className={refreshing ? styles.Spin : undefined} />
            <span>Refresh</span>
          </Button>
        </div>
      </header>

      {error && <Alert variant="warning" className={styles.Alert}>{error}</Alert>}

      <section className={`${styles.WorkerBand} ${statusClass}`} aria-label="Worker process status">
        <div className={styles.WorkerIdentity}>
          <span className={styles.StatusLight} aria-hidden="true" />
          <div>
            <div className={styles.StatusLine}>
              <h2>{worker?.status ?? 'Unknown'}</h2>
              {worker?.isOnline && <Badge bg="success">LIVE</Badge>}
            </div>
            <p>{worker?.machineName ?? 'No worker session has reported yet'}</p>
          </div>
        </div>
        <div className={styles.SessionFacts}>
          <div>
            <span>Process</span>
            <strong>{worker?.processId ?? 'Not available'}</strong>
          </div>
          <div>
            <span>Session started</span>
            <strong>{formatDateTime(worker?.startedAtUtc)}</strong>
          </div>
          <div>
            <span>Heartbeat</span>
            <strong>{formatAge(worker?.heartbeatAgeSeconds ?? null)}</strong>
          </div>
          <div>
            <span>Uptime</span>
            <strong>{worker?.isOnline && worker.startedAtUtc ? formatDuration(worker.startedAtUtc) : 'Not active'}</strong>
          </div>
        </div>
      </section>

      {activeTask && (
        <section className={styles.ActiveTask}>
          <Activity size={17} />
          <span>Currently running</span>
          <strong>{activeTask.name}</strong>
          <span className={styles.ActiveTaskMeta}>
            Started {formatDateTime(activeTask.lastRunStartedAtUtc)}
            {activeTask.lastRunStartedAtUtc && <b><Clock3 size={13} /> {formatElapsed(activeTask.lastRunStartedAtUtc, clockNow)}</b>}
          </span>
        </section>
      )}

      <section className={styles.Metrics} aria-label="Recent metrics">
        <Metric label="Enabled schedules" value={enabledTasks.length} icon={<CalendarClock size={17} />} />
        <Metric label="Running / retrying" value={`${runningTasks.length} / ${retryingTasks.length}`} icon={<Activity size={17} />} />
        <Metric label={`Success in latest ${attempts.length}`} value={successRate === null ? 'No data' : `${successRate}%`} icon={<CircleCheck size={17} />} />
        <Metric label="Failed attempts in sample" value={failedAttempts} icon={<CircleX size={17} />} />
      </section>

      <div className={styles.ContentGrid}>
        <section className={styles.Section}>
          <SectionHeading title="Upcoming schedules" detail="Next 8 enabled tasks" linkTo="/admin/automate" linkText="Manage schedules" />
          <div className={styles.TableWrap}>
            <Table responsive hover size="sm" className={styles.Table}>
              <thead><tr><th>Task</th><th>Status</th><th>Next run</th><th>Last completed</th></tr></thead>
              <tbody>
                {scheduledTasks.map((task) => (
                  <tr key={task.id}>
                    <td className={styles.PrimaryCell}>{task.name}</td>
                    <td><StatusBadge status={task.status} /></td>
                    <td>{formatDateTime(task.nextRunAtUtc)}</td>
                    <td>{formatDateTime(task.lastRunCompletedAtUtc)}</td>
                  </tr>
                ))}
                {!scheduledTasks.length && <EmptyRow colSpan={4} loading={!hasLoaded} label="No enabled schedules" />}
              </tbody>
            </Table>
          </div>
        </section>

        <section className={styles.Section}>
          <SectionHeading title="Recent task runs" detail="Latest 20 runs" linkTo="/admin/task-runs" linkText="View run history" />
          <div className={styles.TableWrap}>
            <Table responsive hover size="sm" className={styles.Table}>
              <thead><tr><th>Task</th><th>Result</th><th>Started</th><th>Attempts</th></tr></thead>
              <tbody>
                {runs.slice(0, 8).map((run) => (
                  <tr key={run.id}>
                    <td className={styles.PrimaryCell}>{run.scheduledTaskName}</td>
                    <td><StatusBadge status={run.status} /></td>
                    <td>{formatDateTime(run.startTimeUtc)}</td>
                    <td>{run.successfulAttemptCount} ok · {run.failedAttemptCount} failed{run.retryCount ? ` · ${run.retryCount} retries` : ''}</td>
                  </tr>
                ))}
                {!runs.length && <EmptyRow colSpan={4} loading={!hasLoaded} label="No task runs recorded" />}
              </tbody>
            </Table>
          </div>
        </section>
      </div>

      <section className={styles.Section}>
        <SectionHeading title="Recent scraper attempts" detail={`Latest ${attempts.length} records`} linkTo="/admin/summary" linkText="Open attempt history" />
        <div className={styles.TableWrap}>
          <Table responsive hover size="sm" className={styles.Table}>
            <thead><tr><th>Scraper</th><th>Origin</th><th>Result</th><th>Started</th><th>Events</th><th>Issue / details</th></tr></thead>
            <tbody>
              {attempts.slice(0, 10).map((attempt) => {
                const running = attempt.errorMessage === 'Running...';
                return (
                  <tr key={attempt.id}>
                    <td className={styles.PrimaryCell}>
                      <Link to={`/admin/scrapers/${attempt.scraperDefinitionId}/runs/${attempt.id}`}>
                        {attempt.scraperName ?? 'Unknown scraper'}
                      </Link>
                    </td>
                    <td><OriginBadge scheduledTaskRunId={attempt.scheduledTaskRunId} /></td>
                    <td>
                      <StatusBadge status={running ? 'Running' : attempt.success ? 'Succeeded' : 'Failed'} />
                      {running && <small className={styles.RunningDuration}>{formatElapsed(attempt.startTimeUtc, clockNow)}</small>}
                    </td>
                    <td>{formatDateTime(attempt.startTimeUtc)}</td>
                    <td>{attempt.extracted} extracted / {attempt.created} created</td>
                    <td className={styles.ErrorCell}>
                      <span>{running ? 'In progress' : attempt.errorMessage || 'No errors recorded'}</span>
                      <Link className={styles.AttemptLink} to={`/admin/scrapers/${attempt.scraperDefinitionId}/runs/${attempt.id}`}>
                        Open attempt <ArrowUpRight size={14} />
                      </Link>
                    </td>
                  </tr>
                );
              })}
              {!attempts.length && <EmptyRow colSpan={6} loading={!hasLoaded} label="No scraper attempts recorded" />}
            </tbody>
          </Table>
        </div>
        {attempts.length > 0 && (
          <p className={styles.SampleNote}>
            Counts above use completed attempts in the latest {attemptPage?.items.length ?? 0} records, not a time-window aggregate.
          </p>
        )}
      </section>

      {!hasLoaded && (
        <div className={styles.Loading}><Spinner size="sm" /> Loading worker data</div>
      )}
    </main>
  );
};

function Metric({ label, value, icon }: { label: string; value: string | number; icon: ReactNode }) {
  return (
    <div className={styles.Metric}>
      <span className={styles.MetricIcon}>{icon}</span>
      <div><span className={styles.MetricLabel}>{label}</span><strong>{value}</strong></div>
    </div>
  );
}

function SectionHeading({ title, detail, linkTo, linkText }: { title: string; detail: string; linkTo: string; linkText: string }) {
  return (
    <div className={styles.SectionHeading}>
      <div><h2>{title}</h2><span>{detail}</span></div>
      <Link to={linkTo} className={styles.SectionLink}>{linkText}<ArrowUpRight size={14} /></Link>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const normalized = status.toLowerCase();
  const variant = normalized.includes('complete') || normalized === 'succeeded' || normalized === 'idle'
    ? 'success'
    : normalized.includes('fail') || normalized === 'error'
      ? 'danger'
      : normalized.includes('retry') || normalized === 'running'
        ? 'warning'
        : 'secondary';
  return <Badge bg={variant} className={styles.Badge}>{status}</Badge>;
}

function OriginBadge({ scheduledTaskRunId }: { scheduledTaskRunId: string | null }) {
  return scheduledTaskRunId
    ? <Badge bg="primary" className={styles.Badge}>Scheduled</Badge>
    : <Badge bg="secondary" className={styles.Badge}>Manual</Badge>;
}

function EmptyRow({ colSpan, loading, label }: { colSpan: number; loading: boolean; label: string }) {
  return <tr><td colSpan={colSpan} className={styles.Empty}>{loading ? 'Loading...' : label}</td></tr>;
}

function formatDateTime(value?: string | null): string {
  if (!value) return 'Not recorded';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not recorded' : date.toLocaleString();
}

function formatClock(value: Date): string {
  return value.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', second: '2-digit' });
}

function formatAge(seconds: number | null): string {
  if (seconds === null) return 'No heartbeat';
  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m ago`;
}

function formatDuration(startedAtUtc: string): string {
  const totalMinutes = Math.max(0, Math.floor((Date.now() - new Date(startedAtUtc).getTime()) / 60_000));
  if (!Number.isFinite(totalMinutes)) return 'Not available';
  if (totalMinutes < 60) return `${totalMinutes}m`;
  return `${Math.floor(totalMinutes / 60)}h ${totalMinutes % 60}m`;
}

function formatElapsed(startedAtUtc: string, now: number): string {
  const seconds = Math.max(0, Math.floor((now - new Date(startedAtUtc).getTime()) / 1000));
  if (!Number.isFinite(seconds)) return 'Not available';
  if (seconds < 60) return `${seconds}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
}

export default WorkerPage;
