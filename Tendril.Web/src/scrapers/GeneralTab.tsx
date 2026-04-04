import { Card, Form } from 'react-bootstrap';

import type { JSX } from 'react';
import { SquareButton as Button } from '../components/button';
import { FormCheck, FormInput, FormSelect, FormText } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, formStyles } from '../styles';
import type {
  ApiParameterSource,
  ApiParameterTarget,
  ExecutionMode,
  ExtractionStrategy,
  Guid,
  HttpMethod,
  PaginationType,
  ScraperDefinition,
  Venue,
} from '../types/api';
import styles from './Tab.module.css';

const toOptions = (arr: string[]) =>
  arr.map((item) => ({ value: item, label: item }));

const executionModeOptions = toOptions(['Static', 'Dynamic', 'Api']);

const extractionStrategyOptions = toOptions([
  'Css',
  'JsonLd',
  'JsonPath',
  'Regex',
  'XPath',
]);

const paginationTypeOptions = toOptions([
  'None',
  'InfiniteScroll',
  'NextButton',
]);

const methodOptions = toOptions(['GET', 'POST']);

const sourceOptions = toOptions(['Static', 'Parent']);

const targetOptions = toOptions(['Query', 'Header', 'Body']);

interface Props {
  scraper: ScraperDefinition;
  venues: Venue[];
  onUpdate: (updated: ScraperDefinition) => void;
  onSave: () => void;
}

export const GeneralTab: React.FC<Props> = ({
  scraper,
  venues,
  onUpdate,
  onSave,
}) => {
  return (
    <Card className={cardStyles.BgCard}>
      <Card.Body>
        <Form className={formStyles.form}>
          <div className={formStyles.formGroup}>
            <FormInput
              className={styles.InputGrow}
              label="Name"
              value={scraper.name}
              onChange={(name) => onUpdate({ ...scraper, name })}
            />

            <FormCheck
              label="Disabled"
              checked={scraper.disabled}
              onChange={(disabled) => onUpdate({ ...scraper, disabled })}
            />
          </div>

          <FormSelect
            label="Venue"
            value={scraper.venueId ?? ''}
            onChange={(venueId) =>
              onUpdate({
                ...scraper,
                venueId: venueId ? (venueId as Guid) : null,
              })
            }
            options={[{ value: '', label: '(none)' }].concat(
              venues.map((v) => ({ value: v.id, label: v.name })),
            )}
          />

          <div className={formStyles.formGroup}>
            <FormInput
              className={styles.InputGrow}
              label="Base URL"
              value={scraper.baseUrl}
              onChange={(baseUrl) => onUpdate({ ...scraper, baseUrl })}
            />
            <Button href={scraper.baseUrl} target="_blank">
              <Icon name="external" />
            </Button>
          </div>

          <FormText
            label="Notes"
            value={scraper.notes}
            onChange={(notes) => onUpdate({ ...scraper, notes })}
          />

          <hr />

          <FormSelect
            label="Execution Mode"
            value={scraper.executionMode}
            onChange={(executionMode) =>
              onUpdate({
                ...scraper,
                executionMode: executionMode as ExecutionMode,
              })
            }
            options={executionModeOptions}
          />
          <FormSelect
            label="Extraction Strategy"
            value={scraper.extractionStrategy}
            onChange={(extractionStrategy) =>
              onUpdate({
                ...scraper,
                extractionStrategy: extractionStrategy as ExtractionStrategy,
              })
            }
            options={extractionStrategyOptions}
          />
          <FormSelect
            label="Paging Type"
            value={scraper.paginationType}
            onChange={(paginationType) =>
              onUpdate({
                ...scraper,
                paginationType: paginationType as PaginationType,
              })
            }
            options={paginationTypeOptions}
          />
          <FormCheck
            label="Use Year Tracking"
            checked={scraper.useYearTracking}
            onChange={(useYearTracking) =>
              onUpdate({
                ...scraper,
                useYearTracking,
              })
            }
          />

          {scraper.executionMode == 'Api' && (
            <>
              <hr />

              <FormSelect
                label="HTTP Method"
                value={scraper.method ?? 'GET'}
                onChange={(method: string) =>
                  onUpdate({
                    ...scraper,
                    method: method as HttpMethod,
                  })
                }
                options={methodOptions}
              />

              <label>Parameters</label>
              <Button
                onClick={() =>
                  onUpdate({
                    ...scraper,
                    parameters: [
                      ...(scraper.parameters || []),
                      {
                        id: '',
                        key: '',
                        template: '',
                        source: 'Parent',
                        target: 'Query',
                        isRequired: false,
                      },
                    ],
                  })
                }
              >
                <Icon name="create" />
              </Button>
              <div className={cardStyles.CardList}>
                {(scraper.parameters ?? []).reduce((prev, param, i, params) => {
                  const row = (
                    <div key={i} className={styles.ParameterRow}>
                      <FormInput
                        label="Key"
                        value={param.key}
                        onChange={(key) =>
                          onUpdate({
                            ...scraper,
                            parameters: scraper.parameters!.map((p, j) =>
                              j === i ? { ...p, key } : p,
                            ),
                          })
                        }
                      />
                      <FormInput
                        label="Template"
                        value={param.template ?? ''}
                        onChange={(template) =>
                          onUpdate({
                            ...scraper,
                            parameters: scraper.parameters!.map((p, j) =>
                              j === i ? { ...p, template } : p,
                            ),
                          })
                        }
                      />
                      <FormSelect
                        label="Source"
                        value={param.source ?? ''}
                        options={sourceOptions}
                        onChange={(source) =>
                          onUpdate({
                            ...scraper,
                            parameters: scraper.parameters!.map((p, j) =>
                              j === i
                                ? { ...p, source: source as ApiParameterSource }
                                : p,
                            ),
                          })
                        }
                      />
                      <FormSelect
                        label="Target"
                        value={param.target ?? ''}
                        options={targetOptions}
                        onChange={(target) =>
                          onUpdate({
                            ...scraper,
                            parameters: scraper.parameters!.map((p, j) =>
                              j === i
                                ? { ...p, target: target as ApiParameterTarget }
                                : p,
                            ),
                          })
                        }
                      />
                      <Button
                        onClick={() =>
                          onUpdate({
                            ...scraper,
                            parameters: scraper.parameters!.filter(
                              (_, j) => j !== i,
                            ),
                          })
                        }
                      >
                        <Icon name="remove" />
                      </Button>
                    </div>
                  );

                  const separator = <hr key={`sep-${i}`} />;

                  return params.length > 1 && i + 1 < params.length
                    ? [...prev, row, separator]
                    : [...prev, row];
                }, [] as JSX.Element[])}
              </div>
            </>
          )}

          <div className={formStyles.buttonRow}>
            <Button onClick={onSave}>Save</Button>
          </div>
        </Form>
      </Card.Body>
    </Card>
  );
};
