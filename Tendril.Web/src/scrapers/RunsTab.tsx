import { format, intervalToDuration, parseISO } from 'date-fns';
import { useState } from 'react';
import { Card, Table } from 'react-bootstrap';

import { ScrapersApi } from '../api/scrapers';
import { SquareButton as Button } from '../components/button';
import { ElapsedClock } from '../components/ElapsedClock';
import styles from '../pages/admin/ScraperEditorPage.module.css';
import { cardStyles, pageStyles, tableStyles } from '../styles';

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
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Test Tools</h3>
      </div>
      <Card className={cardStyles.BgCard}>
        <Card.Body>
          <div className={styles.ButtonContainer}>
            <Button disabled={loading} onClick={() => run('actions')}>
              Test Actions
            </Button>
            {/* <Button disabled={loading} onClick={() => run('mapping')}>
              Test Mapping
            </Button> */}
            <Button disabled={loading} onClick={() => run('test')}>
              Test Actions + Mapping
            </Button>
            <Button disabled={loading} onClick={() => run('now')}>
              Run Now
            </Button>
          </div>

          {loading && <ElapsedClock runStart={runStart} />}

          {result && (
            <div className="run-result">
              <p>
                Success: <strong>{result.success ? 'Yes' : 'No'}</strong>
              </p>
              {result.error && <p className="error">Error: {result.error}</p>}
              <details>
                <summary>Raw</summary>
                <pre>{JSON.stringify(result.raw, null, 2)}</pre>
              </details>
              <details>
                <summary>Mapped</summary>
                <pre>{JSON.stringify(result.mapped, null, 2)}</pre>
              </details>
            </div>
          )}
        </Card.Body>
      </Card>

      <div className={pageStyles.pageHeader}>
        <h3>Runs</h3>
      </div>
      <Card className={cardStyles.BgCard}>
        <Card.Body>
          <Table className={tableStyles.Table} hover responsive>
            <thead>
              <tr>
                <th>Start</th>
                {/* <th>End</th> */}
                <th>Duration</th>
                <th>Success</th>
                <th>Extracted</th>
                <th>Mapped</th>
                <th>Created</th>
                <th>Updated</th>
                <th>Skipped</th>
                <th>Errored</th>
                <th>Error Message</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {attempts.map((a) => (
                <>
                  <tr key={a.id}>
                    <td>
                      <code>
                        {format(new Date(a.startTimeUtc ?? ''), 'yyyy-MM-dd')}{' '}
                        <small className="text-muted">
                          {format(new Date(a.startTimeUtc ?? ''), 'hh:mm')}
                        </small>
                      </code>
                    </td>
                    {/* <td>
                    {!!a.endTimeUtc && new Date(a.endTimeUtc).toLocaleString()}
                  </td> */}
                    <td>
                      {!!a.endTimeUtc &&
                        intervalToDuration({
                          start: parseISO(a.startTimeUtc),
                          end: parseISO(a.endTimeUtc),
                        }).seconds}
                      s
                    </td>
                    <td>{a.success ? '✅ Yes' : '❌ No'}</td>
                    <td>{a.extracted}</td>
                    <td>{a.mapped}</td>
                    <td>{a.created}</td>
                    <td>{a.updated}</td>
                    <td>{a.skipped}</td>
                    <td>{a.errored}</td>
                    <td>{a.errorMessage}</td>
                    <td>
                      {a.logEntries.length > 0 && (
                        <Button
                          variant="outline-primary"
                          size="sm"
                          onClick={() => setSelectedAttemptId(a.id)}
                        >
                          {selectedAttemptId === a.id ? 'Hide' : 'View'} Logs
                        </Button>
                      )}
                    </td>
                  </tr>
                  {selectedAttemptId === a.id &&
                    selectedAttempt?.logEntries.map((l, i) => (
                      <tr key={i}>
                        <td colSpan={11}>
                          <pre>{l.message}</pre>
                        </td>
                      </tr>
                    ))}
                </>
              ))}
            </tbody>
          </Table>
        </Card.Body>
      </Card>
    </>
  );
};
