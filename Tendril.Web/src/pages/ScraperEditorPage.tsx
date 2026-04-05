import React, { useCallback, useEffect, useState } from 'react';
import { ButtonGroup, Dropdown, DropdownButton, Tab } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router';
import { CategoriesApi } from '../api/categories';
import { EventsApi } from '../api/events';
import { ScrapersApi } from '../api/scrapers';
import { VenuesApi } from '../api/venues';
import { SquareButton as Button } from '../components/button';
import { Icon } from '../components/Icon';
import {
  GeneralTab,
  MappingRulesTab,
  RunsTab,
  ActionsTab,
  SummaryTab,
} from '../scrapers';
import { ClassificationRulesTab } from '../scrapers/ClassificationRulesTab';
import { OutputTab } from '../scrapers/OutputTab';
import { pageStyles } from '../styles';
import type {
  ApiParameter,
  Category,
  Event,
  Guid,
  ScraperAttemptHistory,
  ScraperDefinition,
  ScraperAction,
  Venue,
} from '../types/api';
import styles from './ScraperEditorPage.module.css';

type TabKey =
  | 'general'
  | 'actions'
  | 'mapping'
  | 'classification'
  | 'runs'
  | 'summary'
  | 'output';

interface ScraperEditorPage {
  authorized: boolean;
  authLoading: boolean;
}

export const ScraperEditorPage: React.FC<ScraperEditorPage> = ({
  authorized,
  authLoading,
}) => {
  const { scraperId, tabId } = useParams();
  const navigate = useNavigate();
  const [events, setEvents] = useState<Event[]>([]);
  const [scraper, setScraper] = useState<ScraperDefinition | null>(null);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const isNew = scraperId === 'new';

  const [parentId, setParentId] = useState<Guid | null>(null);
  const [parentActions, setParentActions] = useState<
    ScraperAction[] | null
  >(null);

  const activeTab = (tabId as TabKey) || 'general';

  const handleTabChange = (key: TabKey) => {
    navigate(`/scrapers/${scraperId}/${key}`);
  };

  /* Events */
  const loadEvents = async () => {
    if (!scraperId) return;

    const events = await EventsApi.getByScraperId(scraperId);
    setEvents(events);
  };
  /* Events */

  /* Actions */
  const [actions, setActions] = useState<ScraperAction[]>([]);

  const loadActions = async () => {
    if (!scraperId) return;
    const data = await ScrapersApi.getActions(scraperId);
    setActions(data);
  };
  /* Actions */

  /* Attempts */
  const [attempts, setAttempts] = useState<ScraperAttemptHistory[]>([]);

  const loadAttemptHistories = async () => {
    if (!scraperId) return;
    const attempts = await ScrapersApi.getAttemptHistories(scraperId);
    setAttempts(attempts);
  };
  /* Attempts */

  /* Categories */
  const [categories, setCategories] = useState<Category[]>([]);

  const loadCategories = useCallback(async (signal?: AbortSignal) => {
    try {
      const result = await CategoriesApi.getAll(signal);

      const sortProp = (c: Category) => c.name;

      const sorted = result.sort((a, b) =>
        sortProp(a).localeCompare(sortProp(b)),
      );

      setCategories(sorted);
    } catch (err) {
      console.error('Failed to fetch categories', err);
    }
  }, []);
  /* Categories */

  // Initial load
  useEffect(() => {
    if (authorized && scraperId !== 'new') {
      var abortController = new AbortController();

      void loadEvents();
      void loadActions();
      void loadAttemptHistories();
      void loadCategories(abortController.signal);
    }
  }, [scraperId, authorized]);

  useEffect(() => {
    const load = async () => {
      setError(null);

      try {
        const [vs, scraper] = await Promise.all([
          VenuesApi.getAll(),
          isNew || !scraperId
            ? Promise.resolve<ScraperDefinition | null>(null)
            : ScrapersApi.getById(scraperId as Guid),
        ]);

        setVenues(vs);
        setScraper(
          scraper ?? {
            id: '' as Guid,
            name: '',
            baseUrl: '',
            isEventFeed: false,
            disabled: false,
            notes: '',
            executionMode: 'Static',
            extractionStrategy: 'Css',
            paginationType: 'None',
            useYearTracking: false,
            state: 'Unknown',
            lastSuccessUtc: null,
            lastFailureUtc: null,
            lastErrorMessage: null,
            venueId: null,
          },
        );
      } catch (e: any) {
        setError(e.message ?? 'Error loading scraper.');
      }
    };

    if (authorized) {
      void load();
    }
  }, [scraperId, isNew, authorized]);

  // Keep parent actions up-to-date
  useEffect(() => {
    const loadParent = async () => {
      const parentActions = await ScrapersApi.getActions(
        parentId as string,
      );

      setParentActions(parentActions);
    };

    if (
      !!parentId &&
      (!parentActions?.length ||
        parentActions?.some((s) => s.scraperDefinitionId != parentId))
    ) {
      void loadParent();
    } else {
      setParentActions(null);
    }
  }, [parentId]);

  const handleSaveGeneral = async () => {
    if (!scraper) return;
    try {
      if (isNew) {
        const created = await ScrapersApi.create({
          name: scraper.name,
          baseUrl: scraper.baseUrl,
          isEventFeed: scraper.isEventFeed,
          disabled: scraper.disabled,
          notes: scraper.notes,
          executionMode: scraper.executionMode,
          extractionStrategy: scraper.extractionStrategy,
          paginationType: scraper.paginationType,
          useYearTracking: scraper.useYearTracking,
          venueId: scraper.venueId,
          method: scraper.method,
          parameters: scraper.parameters?.map(
            (p) =>
              ({
                ...p,
                id: !p.id ? null : p.id,
              }) as ApiParameter,
          ),
        });
        navigate(`/scrapers/${created.id}`);
      } else if (scraperId) {
        await ScrapersApi.update(scraperId as Guid, {
          name: scraper.name,
          baseUrl: scraper.baseUrl,
          isEventFeed: scraper.isEventFeed,
          disabled: scraper.disabled,
          notes: scraper.notes,
          executionMode: scraper.executionMode,
          extractionStrategy: scraper.extractionStrategy,
          paginationType: scraper.paginationType,
          useYearTracking: scraper.useYearTracking,
          venueId: scraper.venueId,
          method: scraper.method,
          parameters: scraper.parameters?.map(
            (p) =>
              ({
                ...p,
                id: !p.id ? null : p.id,
              }) as ApiParameter,
          ),
        });
      }
      alert('Saved.');
    } catch (e: any) {
      alert(e.message ?? 'Save failed.');
    }
  };

  if (authLoading) return <div>Checking session...</div>;
  if (!authorized) return <></>;

  if (!scraper) return <p>Loading…</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        {/* <h2>{isNew ? 'New Scraper' : `Edit Scraper – ${scraper.name}`}</h2> */}
        <h2>{scraper.name}</h2>
        <Button onClick={() => navigate('/scrapers')}>Back</Button>
      </div>

      <Tab.Container activeKey={activeTab}>
        <div className={styles.Header}>
          <div className={styles.ButtonContainer}>
            <Button
              variant={activeTab == 'general' ? 'active' : 'default'}
              onClick={() => handleTabChange('general')}
            >
              General
            </Button>
            <Button
              variant={activeTab == 'actions' ? 'active' : 'default'}
              disabled={isNew}
              onClick={() => handleTabChange('actions')}
            >
              Actions
            </Button>
            <Button
              variant={activeTab == 'mapping' ? 'active' : 'default'}
              disabled={isNew}
              onClick={() => handleTabChange('mapping')}
            >
              Mapping
            </Button>
            <Button
              variant={activeTab == 'classification' ? 'active' : 'default'}
              disabled={isNew}
              onClick={() => handleTabChange('classification')}
            >
              Classification
            </Button>
            <Button
              variant={activeTab == 'summary' ? 'active' : 'default'}
              disabled={isNew}
              onClick={() => handleTabChange('summary')}
            >
              Summary
            </Button>
            <Button
              variant={activeTab == 'runs' ? 'active' : 'default'}
              disabled={isNew}
              onClick={() => handleTabChange('runs')}
            >
              Runs
            </Button>
            <Button
              variant={activeTab == 'output' ? 'active' : 'default'}
              disabled={isNew}
              onClick={() => handleTabChange('output')}
            >
              Output
            </Button>
          </div>

          {!!scraper.parents && scraper.parents.length > 0 && (
            <div className={styles.ButtonContainer}>
              <DropdownButton
                title={
                  scraper.parents.find((s) => s.id == parentId)?.name ??
                  `(${scraper.parents.length}) reference${scraper.parents.length > 1 ? 's' : ''}`
                }
                as={ButtonGroup}
                variant="outline-secondary"
              >
                <Dropdown.Item onClick={() => setParentId(null)}>
                  {'\<None\>'}
                </Dropdown.Item>
                {scraper.parents.map((s) => (
                  <Dropdown.Item onClick={() => setParentId(s.id)}>
                    {s.name}
                  </Dropdown.Item>
                ))}
              </DropdownButton>
              <Button
                disabled={!parentId}
                onClick={() => {
                  setParentActions(null);
                  navigate(`/scrapers/${parentId}/actions`);
                  setParentId(null);
                }}
              >
                <Icon name="external" />
              </Button>
            </div>
          )}
        </div>

        <Tab.Content>
          <Tab.Pane eventKey="general">
            <GeneralTab
              scraper={scraper}
              venues={venues}
              onSave={handleSaveGeneral}
              onUpdate={setScraper}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="actions">
            <ActionsTab
              scraper={scraper}
              actions={actions}
              parentId={parentId}
              parentActions={parentActions}
              refresh={loadActions}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="mapping">
            <MappingRulesTab
              scraperId={scraperId as Guid}
              actions={actions}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="classification">
            <ClassificationRulesTab
              scraperId={scraperId as Guid}
              categories={categories}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="summary">
            <SummaryTab scraperId={scraperId as Guid} />
          </Tab.Pane>

          <Tab.Pane eventKey="runs">
            <RunsTab
              scraperId={scraperId as Guid}
              attempts={attempts}
              onComplete={() => {
                loadEvents();
                loadAttemptHistories();
              }}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="output">
            <OutputTab
              scraperId={scraperId as Guid}
              categories={categories}
              events={events}
              loadEvents={loadEvents}
            />
          </Tab.Pane>
        </Tab.Content>
      </Tab.Container>
    </section>
  );
};
