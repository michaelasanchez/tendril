import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Table } from 'react-bootstrap';
import { useNavigate } from 'react-router';
import { ScrapersApi } from '../../api/scrapers';
import { SquareButton } from '../../components/button';
import { Icon } from '../../components/Icon';
import { useLocalStorage } from '../../hooks';
import { pageStyles, tableStyles } from '../../styles';
import type { ScraperDefinition } from '../../types/api';

type SortKey =
  | 'name'
  | 'baseUrl'
  | 'state'
  | 'lastSuccessUtc'
  | 'lastFailureUtc'
  | 'pendingEvents';

type SortDirection = 'asc' | 'desc';

interface ScrapersPageProps {}

interface Sort {
  key: SortKey;
  direction: SortDirection;
}

const sortScrapers = (
  scrapers: ScraperDefinition[],
  sort: Sort | null,
): ScraperDefinition[] => {
  if (!sort) return scrapers;

  const direction = sort.direction === 'asc' ? 1 : -1;

  const compareNullable = <T,>(
    aValue: T | null | undefined,
    bValue: T | null | undefined,
    compare: (a: T, b: T) => number,
  ) => {
    if (aValue == null) return bValue == null ? 0 : 1;
    if (bValue == null) return -1;
    return compare(aValue, bValue) * direction;
  };

  return [...scrapers].sort((a, b) => {
    switch (sort.key) {
      case 'name':
        return compareNullable(a.name, b.name, (aName, bName) =>
          aName
            .replace(/^The /i, '')
            .localeCompare(bName.replace(/^The /i, '')),
        );
      case 'baseUrl':
        return compareNullable(a.baseUrl, b.baseUrl, (aUrl, bUrl) =>
          aUrl.localeCompare(bUrl),
        );
      case 'state':
        return compareNullable(a.state, b.state, (aState, bState) =>
          aState.localeCompare(bState),
        );
      case 'pendingEvents':
        return compareNullable(
          a.pendingEvents,
          b.pendingEvents,
          (aCount, bCount) => aCount - bCount,
        );
      case 'lastSuccessUtc':
      case 'lastFailureUtc':
        return compareNullable(a[sort.key], b[sort.key], (aDate, bDate) => {
          const aTime = new Date(aDate).getTime();
          const bTime = new Date(bDate).getTime();
          const aInvalid = Number.isNaN(aTime);
          const bInvalid = Number.isNaN(bTime);

          if (aInvalid || bInvalid) {
            return aInvalid === bInvalid
              ? 0
              : aInvalid
                ? -direction
                : direction;
          }
          return aTime - bTime;
        });
    }
  });
};

const sortIndicator = (direction: SortDirection | undefined) =>
  direction !== undefined ? <>&nbsp;{direction === 'asc' ? '▲' : '▼'}</> : '';

const TableHeader: React.FC<{
  label: string;
  sort: Sort | null;
  sortKey: SortKey;
  onSort: (key: SortKey) => void;
}> = ({ label, sort, sortKey, onSort }) => {
  return (
    <th onClick={() => onSort(sortKey)}>
      {label} {sort?.key == sortKey && sortIndicator(sort.direction)}
    </th>
  );
};

export const ScrapersPage: React.FC<ScrapersPageProps> = ({}) => {
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sortStorage = useLocalStorage('sort');

  const hasLoaded = useRef(false);

  const [sort, setSort] = useState<Sort | null>(
    sortStorage.exists()
      ? JSON.parse(sortStorage.fetch()!)
      : {
          key: 'name',
          direction: 'asc',
        },
  );

  useEffect(() => {
    if (!sort) return;
    sortStorage.commit(JSON.stringify(sort));
  }, [sort]);

  const onSort = (key: SortKey) => {
    setSort((prev) => {
      if (!prev || prev.key !== key) {
        return { key, direction: 'asc' };
      }

      return {
        key,
        direction: prev.direction === 'asc' ? 'desc' : 'asc',
      };
    });
  };

  const navigate = useNavigate();

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await ScrapersApi.getAll();
      setScrapers(data);
    } catch (e: any) {
      setError(e.message ?? 'Error loading scrapers.');
    } finally {
      setLoading(false);
    }
  };

  // Initial load
  useEffect(() => {
    if (hasLoaded.current) return;

    hasLoaded.current = true;
    void load();
  }, []);

  // const handleRunNow = async (id: string) => {
  //   if (!window.confirm('Run this scraper now?')) return;
  //   try {
  //     await ScrapersApi.runNow(id);
  //     await load();
  //   } catch (e: any) {
  //     alert(e.message ?? 'Run failed.');
  //   }
  // };

  const scraperGroups = useMemo(() => {
    // 1. Create temporary buckets
    const feedList: ScraperDefinition[] = [];
    const otherList: ScraperDefinition[] = [];
    // const strategyMap = new Map<string, ScraperDefinition[]>();
    // const unknownList: ScraperDefinition[] = [];
    const disabledList: ScraperDefinition[] = [];

    // 2. Sort items into buckets (Single pass)
    scrapers.forEach((scraper) => {
      if (scraper.disabled) {
        disabledList.push(scraper);
        return;
      }

      if (scraper.isEventFeed) {
        feedList.push(scraper);
        return;
      }

      otherList.push(scraper);
    });

    // 4. Construct Final List (Strategies -> Unknown -> Disabled)
    const result = [
      { key: 'Event Feeds', scrapers: sortScrapers(feedList, sort) },
    ];

    if (otherList.length > 0) {
      result.push({
        key: 'Component',
        scrapers: sortScrapers(otherList, sort),
      });
    }

    if (disabledList.length > 0) {
      result.push({
        key: 'Disabled',
        scrapers: sortScrapers(disabledList, sort),
      });
    }

    return result;
  }, [scrapers, sort]);

  return (
    <section>
      <>
        <div className={pageStyles.pageHeader}>
          <h2>Scrapers</h2>
          <SquareButton onClick={() => navigate('/admin/scrapers/new')}>
            New Scraper
          </SquareButton>
        </div>
        {loading && <p>Loading…</p>}
        {error && <p className="error">{error}</p>}
        <Table className={tableStyles.Table} hover responsive>
          <thead>
            <tr>
              <TableHeader
                label="Name"
                sort={sort}
                sortKey="name"
                onSort={onSort}
              />
              <TableHeader
                label="Base URL"
                sort={sort}
                sortKey="baseUrl"
                onSort={onSort}
              />
              <TableHeader
                label="State"
                sort={sort}
                sortKey="state"
                onSort={onSort}
              />
              <th></th>
              <TableHeader
                label="Last Success"
                sort={sort}
                sortKey="lastSuccessUtc"
                onSort={onSort}
              />
              <TableHeader
                label="Last Failure"
                sort={sort}
                sortKey="lastFailureUtc"
                onSort={onSort}
              />
              <TableHeader
                label="Pending"
                sort={sort}
                sortKey="pendingEvents"
                onSort={onSort}
              />
            </tr>
          </thead>

          {scraperGroups.map(({ key, scrapers }) => (
            <tbody key={key}>
              <tr className={tableStyles.GroupHeader}>
                <td colSpan={7}>
                  {key} ({scrapers?.length})
                </td>
              </tr>

              {scrapers.map((s) => (
                <tr
                  key={s.id}
                  className={tableStyles.Clickable}
                  onClick={() => navigate(`/admin/scrapers/${s.id}`)}
                >
                  <td>{s.name}</td>
                  <td>
                    <a href={s.baseUrl} target="_blank">
                      {s.baseUrl}
                    </a>
                  </td>
                  <td>
                    <div>{s.isEventFeed ? s.state : '-'}</div>
                    {s.stateMessage && (
                      <small className="text-muted">({s.stateMessage})</small>
                    )}
                  </td>
                  <td>
                    {s.hasSuggestions && (
                      <>
                        &nbsp;
                        <Icon name="suggestions" />
                      </>
                    )}
                    {s.requiresReview && (
                      <>
                        &nbsp;
                        <Icon name="flag" />
                      </>
                    )}
                  </td>
                  <td>
                    {s.lastSuccessUtc ? formatDate(s.lastSuccessUtc) : '-'}
                  </td>
                  <td>
                    {s.lastFailureUtc ? formatDate(s.lastFailureUtc) : '-'}
                  </td>
                  <td>{s.pendingEvents}</td>
                  {/* <td className={tableStyles.TableActions}>
                    <div>
                      <Button
                        onClick={() => navigate(`/admin/scrapers/${s.id}`)}
                      >
                        <Icon name="edit" />
                      </Button>
                      {s.isEventFeed && (
                        <Button
                          variant="outline-primary"
                          onClick={() => handleRunNow(s.id)}
                        >
                          <Icon name="run" />
                        </Button>
                      )}
                    </div>
                  </td> */}
                </tr>
              ))}
              {scrapers.length === 0 && !loading && (
                <tr>
                  <td colSpan={6}>No scrapers defined yet.</td>
                </tr>
              )}
            </tbody>
          ))}
        </Table>
      </>
    </section>
  );

  function formatDate(dateString: string) {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }
};

export default ScrapersPage;
