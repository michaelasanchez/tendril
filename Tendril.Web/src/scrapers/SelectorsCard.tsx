import cn from 'classnames';
import { Card, Table } from 'react-bootstrap';
import { useNavigate } from 'react-router';
import { SquareButton as Button } from '../components/button';
import { FormCheck } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, tableStyles } from '../styles';
import type { ScraperSelector } from '../types/api';

export interface ScraperOption {
  label: string;
  value: string;
}

interface Props {
  disabled?: boolean;
  scraperOptions?: ScraperOption[];
  selectors: ScraperSelector[];
  onDisable?: (scraper: ScraperSelector) => void;
  onEdit?: (scraper: ScraperSelector) => void;
  onRemove?: (scraper: ScraperSelector) => void;
}

export const SelectorsCard: React.FC<Props> = ({
  disabled,
  scraperOptions,
  selectors,
  onDisable,
  onEdit,
  onRemove,
}) => {
  const navigate = useNavigate();

  return (
    <Card
      className={cn(
        cardStyles.BgCard,
        cardStyles.MarginBottom,
        disabled && cardStyles.Muted,
      )}
    >
      <Card.Body>
        <Table className={tableStyles.Table} hover responsive>
          <thead>
            <tr>
              <th>Field</th>
              <th>Selector</th>
              <th>Order</th>
              <th>Root</th>
              <th>Type</th>
              <th>Attribute</th>
              <th>Delay</th>
              <th>Value</th>
              <th>Interaction</th>
              <th>Child Scraper</th>
              <th>Pagination Trigger</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {selectors
              .sort((a, b) => a.order - b.order)
              .map((s) => (
                <tr
                  key={s.id}
                  className={s.disabled ? tableStyles.Disabled : ''}
                >
                  <td>{s.fieldName}</td>
                  <td>
                    <code>{s.selector}</code>
                  </td>
                  <td>{s.order}</td>
                  <td>{s.root ? 'Yes' : ''}</td>
                  <td>{s.type}</td>
                  <td>{s.attribute}</td>
                  <td>{s.delay}</td>
                  <td>{s.constantValue}</td>
                  <td>{s.interactionValue}</td>
                  <td>
                    {s.childScraperId && (
                      <>
                        <div>
                          {
                            scraperOptions?.find(
                              (o) => o.value === s.childScraperId,
                            )?.label
                          }{' '}
                          {disabled ? (
                            ''
                          ) : (
                            <Button
                              onClick={() =>
                                navigate(
                                  `/scrapers/${s.childScraperId}/selectors`,
                                )
                              }
                            >
                              <Icon name="external" />
                            </Button>
                          )}
                        </div>
                        <div>
                          <FormCheck
                            label="Ignore Duplicate Urls"
                            checked={s.ignoreDuplicateUrls}
                            disabled
                            readonly
                          />
                        </div>
                      </>
                    )}
                  </td>
                  <td>{s.isPaginationTrigger ? 'Yes' : ''}</td>
                  {!disabled && (
                    <td className={tableStyles.TableActions}>
                      <div>
                        {onEdit && (
                          <Button onClick={() => onEdit(s)}>
                            <Icon name="edit" />
                          </Button>
                        )}
                        {onDisable && (
                          <Button onClick={() => onDisable(s)}>
                            <Icon name={s.disabled ? 'disabled' : 'enable'} />
                          </Button>
                        )}
                        {onRemove && (
                          <Button
                            variant="outline-danger"
                            onClick={() => onRemove(s)}
                          >
                            <Icon name="remove" />
                          </Button>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            {selectors.length === 0 && (
              <tr>
                <td colSpan={5}>No selectors defined.</td>
              </tr>
            )}
          </tbody>
        </Table>
      </Card.Body>
    </Card>
  );
};
