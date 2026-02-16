import cn from 'classnames';
import React, { useEffect, useState } from 'react';
import { Card, Form, Table } from 'react-bootstrap';
import { ScrapersApi } from '../api/scrapers';
import { SquareButton as Button } from '../components/button';
import { FormCheck, FormInput, FormSelect } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, formStyles, pageStyles, tableStyles } from '../styles';
import type { Guid, ScraperSelector, SelectorType } from '../types/api';

interface Props {
  scraperId: Guid;
  selectors: ScraperSelector[];
  refresh: () => Promise<void>;
}

const toOptions = (arr: string[]) =>
  arr.map((item) => ({ value: item, label: item }));

const selectorTypeOptions = toOptions([
  'Container',
  'Text',
  'Attribute',
  'Click',
  'Hover',
  'Scroll',
  'Input',
  'CaptureLink',
  'FollowLink',
]);

interface Option {
  label: string;
  value: string;
}

export const ScraperSelectorsTab: React.FC<Props> = ({
  scraperId,
  selectors,
  refresh: load,
}) => {
  const [editing, setEditing] = useState<Partial<ScraperSelector>>({});
  const [isNew, setIsNew] = useState(false);

  const [scraperOptions, setScraperOptions] = useState<Option[]>([]);

  useEffect(() => {
    if (
      selectors.some((s) => s.type === 'FollowLink') ||
      editing.type === 'FollowLink'
    ) {
      const loadScrapers = async () => {
        const data = await ScrapersApi.getAll();
        const options = data.map((s) => ({ label: s.name, value: s.id }));
        setScraperOptions(options);
      };

      void loadScrapers();
    }
  }, [selectors, editing]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      fieldName: '',
      selector: '',
      order: selectors.length,
      root: false,
      type: 'Text',
      attribute: null,
      delay: null,
    } as Partial<ScraperSelector>);
  };

  const startEdit = (sel: ScraperSelector) => {
    setIsNew(false);
    setEditing({ ...sel });
  };

  const cancelEdit = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    if (!editing.fieldName || !editing.type) return;

    if (isNew) {
      await ScrapersApi.createSelector(scraperId, {
        fieldName: editing.fieldName,
        selector: editing.selector ?? '',
        order: editing.order ?? selectors.length,
        root: editing.root ?? false,
        type: editing.type,
        attribute:
          editing.type == 'Attribute' && !!editing.attribute
            ? editing.attribute
            : null,
        delay: editing.delay ?? null,
        interactionValue: editing.interactionValue ?? null,
        childScraperId: editing.childScraperId ?? null,
        isPaginationTrigger: editing.isPaginationTrigger ?? false,
        disabled: editing.disabled ?? false,
      });
    } else if (editing.id) {
      await ScrapersApi.updateSelector(scraperId, editing.id, {
        fieldName: editing.fieldName,
        selector: editing.selector,
        order: editing.order,
        root: editing.root,
        type: editing.type,
        attribute:
          editing.type == 'Attribute' && !!editing.attribute
            ? editing.attribute
            : null,
        delay: editing.delay ?? null,
        interactionValue: editing.interactionValue ?? null,
        childScraperId: editing.childScraperId ?? null,
        isPaginationTrigger: editing.isPaginationTrigger ?? false,
        disabled: editing.disabled ?? false,
      });
    }
    await load();
    cancelEdit();
  };

  const remove = async (sel: ScraperSelector) => {
    if (!window.confirm(`Delete selector "${sel.fieldName}"?`)) return;
    await ScrapersApi.deleteSelector(scraperId, sel.id);
    await load();
  };

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Selectors</h3>
        <Button variant="primary" onClick={startNew}>
          Add Selector
        </Button>
      </div>
      <Card className={cn(cardStyles.BgCard, cardStyles.MarginBottom)}>
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
                    <td>{s.interactionValue}</td>
                    <td>
                      {s.childScraperId &&
                        scraperOptions.find((o) => o.value === s.childScraperId)
                          ?.label}
                    </td>
                    <td>{s.isPaginationTrigger ? 'Yes' : ''}</td>
                    <td className={tableStyles.TableActions}>
                      <div>
                        <Button onClick={() => startEdit(s)}>
                          <Icon name="edit" />
                        </Button>
                        <Button
                          variant="outline-danger"
                          onClick={() => remove(s)}
                        >
                          <Icon name="remove" />
                        </Button>
                      </div>
                    </td>
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

      {editing.fieldName !== undefined && (
        <>
          <h4>{isNew ? 'New Selector' : 'Edit Selector'}</h4>
          <Card className={cardStyles.BgCard}>
            <Card.Body>
              <Form className={formStyles.form}>
                <FormInput
                  label="Field Name"
                  value={editing.fieldName ?? ''}
                  autoFocus={true}
                  onChange={(fieldName) =>
                    setEditing({ ...editing, fieldName })
                  }
                />
                <FormInput
                  label="Selector"
                  value={editing.selector ?? ''}
                  onChange={(selector) => setEditing({ ...editing, selector })}
                />
                <FormInput
                  type="number"
                  label="Order"
                  value={editing.order?.toString() ?? '0'}
                  onChange={(order) =>
                    setEditing({ ...editing, order: parseInt(order) })
                  }
                />
                <div className={formStyles.formGroup}>
                  <FormCheck
                    label="Root"
                    checked={editing.root ?? false}
                    onChange={(checked) =>
                      setEditing({ ...editing, root: checked })
                    }
                  />
                  <FormCheck
                    label="Pagination Trigger"
                    checked={editing.isPaginationTrigger ?? false}
                    onChange={(isPaginationTrigger) =>
                      setEditing({ ...editing, isPaginationTrigger })
                    }
                  />
                </div>
                <FormSelect
                  label="Type"
                  value={editing.type ?? 'Text'}
                  onChange={(value) =>
                    setEditing({ ...editing, type: value as SelectorType })
                  }
                  options={selectorTypeOptions}
                />
                {editing.type === 'Attribute' && (
                  <FormInput
                    label="Attribute"
                    value={editing.attribute ?? ''}
                    onChange={(attribute) =>
                      setEditing({ ...editing, attribute })
                    }
                  />
                )}
                <FormInput
                  type="number"
                  label="Delay"
                  value={editing.delay?.toString() ?? ''}
                  onChange={(delay) =>
                    setEditing({ ...editing, delay: parseInt(delay) })
                  }
                />
                {editing.type === 'Input' && (
                  <FormInput
                    label="Interaction Value"
                    value={editing.interactionValue ?? ''}
                    onChange={(interactionValue) =>
                      setEditing({ ...editing, interactionValue })
                    }
                  />
                )}
                {editing.type == 'FollowLink' && (
                  <FormSelect
                    label="Child Scraper"
                    value={editing.childScraperId ?? ''}
                    options={scraperOptions}
                    onChange={(childScraperId) =>
                      setEditing({ ...editing, childScraperId })
                    }
                  />
                )}
                <FormCheck
                  label="Disabled"
                  checked={editing.disabled ?? false}
                  onChange={(disabled) => setEditing({ ...editing, disabled })}
                />
                <div className={formStyles.buttonRow}>
                  <Button variant="primary" onClick={save}>
                    Save
                  </Button>
                  <Button onClick={cancelEdit}>Cancel</Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </>
      )}
    </>
  );
};
