import type { Guid, ScraperSummary } from '../types/api';

import cn from 'classnames';
import { useCallback, useEffect, useState } from 'react';
import { Card } from 'react-bootstrap';
import { ActionsApi } from '../api/scrapers';
import { cardStyles, pageStyles } from '../styles';

interface Props {
  scraperId: Guid;
}

export const SummaryTab: React.FC<Props> = ({ scraperId }) => {
  const [summary, setSummary] = useState<ScraperSummary>();

  const loadTags = useCallback(async (signal?: AbortSignal) => {
    try {
      const summary = await ActionsApi.getScraperSummary(scraperId, signal);

      setSummary(summary);
    } catch (err) {
      console.error('Failed to fetch tags', err);
    }
  }, []);

  const load = async () => {
    const abortController = new AbortController();

    try {
      await Promise.all([loadTags(abortController.signal)]);
    } catch (err) {
      console.error('Failed to load classification rules data', err);
    }
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Summary</h3>
      </div>
      <Card className={cn(cardStyles.BgCard, cardStyles.MarginBottom)}>
        <Card.Body>
          <h6>Mapping</h6>
          {summary?.mapping ? (
            <pre>{JSON.stringify(summary.mapping, null, 2)}</pre>
          ) : (
            <p>No mapping data available.</p>
          )}
        </Card.Body>
      </Card>
    </>
  );
};
