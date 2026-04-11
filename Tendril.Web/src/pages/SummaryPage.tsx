import cn from 'classnames';
import React, { useCallback, useEffect, useState } from 'react';
import { ScrapersApi } from '../api/scrapers';
import type { ScraperSummary } from '../types/api';
import styles from './SummaryPage.module.css';

interface Props {}

interface SummaryKey {
  key: string;
  priority: number;
}

const summaryKeys: SummaryKey[] = [
  { key: 'title', priority: 1 },
  { key: 'description', priority: 1 },
  { key: 'location', priority: 1 },
  { key: 'venue', priority: 1 },
  { key: 'startUtc', priority: 1 },
  { key: 'endUtc', priority: 2 },
  { key: 'minPrice', priority: 2 },
  { key: 'maxPrice', priority: 2 },
  { key: 'detailsUrl', priority: 1 },
  { key: 'imageUrl', priority: 1 },
  { key: 'ticketUrl', priority: 1 },
];

const stickyStyle: React.CSSProperties = {};

export const SummaryPage: React.FC<Props> = () => {
  const [summaries, setSummaries] = useState<ScraperSummary[]>([]);

  const load = async () => {
    const abortController = new AbortController();

    try {
      await Promise.all([loadSummaries(abortController.signal)]);
    } catch (err) {
      console.error('Failed to load classification rules data', err);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const loadSummaries = useCallback(async (signal?: AbortSignal) => {
    try {
      const summaries = await ScrapersApi.getFeedSummaries(signal);

      setSummaries(
        summaries.sort((a, b) =>
          a.name.replace('The ', '').localeCompare(b.name.replace('The ', '')),
        ),
      );
    } catch (err) {
      console.error('Failed to fetch classification rules', err);
    }
  }, []);

  return (
    <div className={styles.Grid}>
      <div className={cn(styles.Cell, styles.Header, styles.Sticky)}>
        Field / Summary
      </div>
      {summaries.map((summary, i) => (
        <div key={`name-${i}`} className={cn(styles.Cell, styles.Header)}>
          {summary.name}
        </div>
      ))}

      {summaryKeys.map((field, j) => (
        <React.Fragment key={field.key}>
          <div className={cn(styles.Cell, styles.Header, styles.Sticky)}>
            {field.key}
          </div>

          {summaries.map((summary, i) => (
            <div key={`${i}-${j}`} className={cn(styles.Cell, styles.Data)}>
              {(summary.mapping as any)[field.key] ? '\u25cf' : ''}
            </div>
          ))}
        </React.Fragment>
      ))}
    </div>
  );
};
