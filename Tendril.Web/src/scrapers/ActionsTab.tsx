import React, { useEffect, useState } from 'react';
import { Card, Form } from 'react-bootstrap';
import { ActionsCard, type ScraperOption } from '.';
import { ScrapersApi } from '../api/scrapers';
import { SquareButton as Button } from '../components/button';
import { FormCheck, FormInput, FormSelect } from '../components/form';
import { Icon } from '../components/Icon';
import { cardStyles, formStyles, pageStyles } from '../styles';
import type {
  ActionType,
  Guid,
  ScraperAction,
  ScraperDefinition,
} from '../types/api';

interface Props {
  scraper: ScraperDefinition;
  parentId: Guid | null;
  actions: ScraperAction[];
  parentActions: ScraperAction[] | null;
  refresh: () => Promise<void>;
}

const toOptions = (arr: string[]) =>
  arr.map((item) => ({ value: item, label: item }));

const selectorTypeOptions = toOptions([
  'Constant',
  'Container',
  'Text',
  'Attribute',
  'Click',
  'Hover',
  'Scroll',
  'Input',
  'CaptureLink',
  'FollowLink',
  'CallApi',
]);

export const ActionsTab: React.FC<Props> = ({
  scraper,
  parentId,
  actions,
  parentActions,
  refresh: load,
}) => {
  const [editing, setEditing] = useState<Partial<ScraperAction>>({});
  const [isNew, setIsNew] = useState(false);
  const [showDisabled, setShowDisabled] = useState(true);

  const [scraperOptions, setScraperOptions] = useState<ScraperOption[]>([]);

  useEffect(() => {
    if (
      !!parentId ||
      actions.some((s) => s.type === 'FollowLink' || s.type === 'CallApi') ||
      editing.type === 'FollowLink'
    ) {
      const loadScrapers = async () => {
        const data = await ScrapersApi.getAll();
        const options = data.map((s) => ({ label: s.name, value: s.id }));
        setScraperOptions(options);
      };

      void loadScrapers();
    }
  }, [actions, editing, parentId]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      fieldName: '',
      selector: '',
      order: actions.length,
      root: false,
      type: 'Text',
      attribute: null,
      delay: null,
    } as Partial<ScraperAction>);
  };

  const startEdit = (sel: ScraperAction) => {
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
      await ScrapersApi.createAction(scraper.id, {
        name: editing.name ?? '',
        fieldName: editing.fieldName,
        selector: editing.selector ?? '',
        order: editing.order ?? actions.length,
        root: editing.root ?? false,
        type: editing.type,
        attribute:
          editing.type == 'Attribute' && !!editing.attribute
            ? editing.attribute
            : null,
        delay: editing.delay ?? null,
        constantValue: editing.constantValue ?? null,
        interactionValue: editing.interactionValue ?? null,
        childScraperId: editing.childScraperId ?? null,
        ignoreDuplicateUrls: editing.ignoreDuplicateUrls ?? true,
        isPaginationTrigger: editing.isPaginationTrigger ?? false,
        disabled: editing.disabled ?? false,
      });
    } else if (editing.id) {
      await ScrapersApi.updateAction(scraper.id, editing.id, {
        name: editing.name,
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
        constantValue: editing.constantValue ?? null,
        interactionValue: editing.interactionValue ?? null,
        childScraperId: editing.childScraperId ?? null,
        ignoreDuplicateUrls: editing.ignoreDuplicateUrls ?? true,
        isPaginationTrigger: editing.isPaginationTrigger ?? false,
        disabled: editing.disabled ?? false,
      });
    }
    await load();
    cancelEdit();
  };

  const toggleDisable = async (action: ScraperAction) => {
    await ScrapersApi.updateAction(scraper.id, action.id, {
      ...action,
      disabled: !action.disabled,
    });
    await load();
  };

  const remove = async (sel: ScraperAction) => {
    if (!window.confirm(`Delete selector "${sel.fieldName}"?`)) return;
    await ScrapersApi.deleteAction(scraper.id, sel.id);
    await load();
  };

  return (
    <>
      <div className={pageStyles.pageHeader}>
        <h3>Actions</h3>
        <div style={{ display: 'flex', gap: '1em' }}>
          {actions.some((a) => a.disabled) && (
            <Button
              variant="outline-primary"
              onClick={() => setShowDisabled(!showDisabled)}
            >
              <Icon name={showDisabled ? 'visible' : 'invisible'} /> Disabled
            </Button>
          )}
          <Button variant="primary" onClick={startNew}>
            <Icon name="create" /> Add
          </Button>
        </div>
      </div>

      {!!parentActions && parentActions.length > 0 && (
        <ActionsCard
          disabled
          scraperOptions={scraperOptions}
          actions={parentActions}
          showDisabled={showDisabled}
        />
      )}

      <ActionsCard
        scraperOptions={scraperOptions}
        actions={actions}
        showDisabled={showDisabled}
        onDisable={toggleDisable}
        onEdit={startEdit}
        // onRemove={remove}
      />

      {editing.fieldName !== undefined && (
        <>
          <h4>{isNew ? 'New Action' : 'Edit Action'}</h4>
          <Card className={cardStyles.BgCard}>
            <Card.Body>
              <Form className={formStyles.form}>
                <FormInput
                  label="Name"
                  value={editing.name ?? ''}
                  onChange={(name) => setEditing({ ...editing, name })}
                />
                <FormInput
                  label="Field Name"
                  value={editing.fieldName ?? ''}
                  autoFocus={isNew}
                  onChange={(fieldName) =>
                    setEditing({ ...editing, fieldName })
                  }
                />
                <div className={formStyles.formGroup}>
                  <FormCheck
                    label="Disabled"
                    checked={editing.disabled ?? false}
                    onChange={(disabled) =>
                      setEditing({ ...editing, disabled })
                    }
                  />
                </div>
                <FormSelect
                  label="Type"
                  value={editing.type ?? 'Text'}
                  onChange={(value) =>
                    setEditing({ ...editing, type: value as ActionType })
                  }
                  options={selectorTypeOptions}
                />
                {editing.type != 'CallApi' &&
                  editing.type != 'ConstantValue' && (
                    <FormInput
                      label="Selector"
                      value={editing.selector ?? ''}
                      autoFocus={!isNew}
                      onChange={(selector) =>
                        setEditing({ ...editing, selector })
                      }
                    />
                  )}
                <div className={formStyles.formGroup}>
                  {editing.type != 'CallApi' &&
                    editing.type != 'ConstantValue' && (
                      <FormCheck
                        label="Root"
                        checked={editing.root ?? false}
                        onChange={(checked) =>
                          setEditing({ ...editing, root: checked })
                        }
                      />
                    )}
                  {editing.type == 'Click' && (
                    <FormCheck
                      label="Pagination Trigger"
                      checked={editing.isPaginationTrigger ?? false}
                      onChange={(isPaginationTrigger) =>
                        setEditing({ ...editing, isPaginationTrigger })
                      }
                    />
                  )}
                  {(editing.type == 'FollowLink' ||
                    editing.type == 'CallApi') && (
                    <FormCheck
                      label="Ignore Duplicate URLs"
                      checked={editing.ignoreDuplicateUrls ?? false}
                      onChange={(ignoreDuplicateUrls) =>
                        setEditing({ ...editing, ignoreDuplicateUrls })
                      }
                    />
                  )}
                </div>
                {editing.type === 'Attribute' && (
                  <FormInput
                    label="Attribute"
                    value={editing.attribute ?? ''}
                    onChange={(attribute) =>
                      setEditing({ ...editing, attribute })
                    }
                  />
                )}
                {editing.type === 'ConstantValue' && (
                  <FormInput
                    label="Constant Value"
                    value={editing.constantValue ?? ''}
                    onChange={(constantValue) =>
                      setEditing({ ...editing, constantValue })
                    }
                  />
                )}
                {editing.type === 'Input' && (
                  <FormInput
                    label="Interaction Value"
                    value={editing.interactionValue ?? ''}
                    onChange={(interactionValue) =>
                      setEditing({ ...editing, interactionValue })
                    }
                  />
                )}
                {(editing.type == 'FollowLink' ||
                  editing.type == 'CallApi') && (
                  <FormSelect
                    label="Child Scraper"
                    value={editing.childScraperId ?? ''}
                    options={scraperOptions}
                    onChange={(childScraperId) =>
                      setEditing({ ...editing, childScraperId })
                    }
                  />
                )}
                <FormInput
                  type="number"
                  label="Order"
                  value={editing.order?.toString() ?? '0'}
                  onChange={(order) =>
                    setEditing({ ...editing, order: parseInt(order) })
                  }
                />
                <FormInput
                  type="number"
                  label="Delay"
                  value={editing.delay?.toString() ?? ''}
                  onChange={(delay) =>
                    setEditing({ ...editing, delay: parseInt(delay) })
                  }
                />
                <div className={formStyles.buttonRow}>
                  <Button onClick={cancelEdit}>Cancel</Button>
                  <Button variant="primary" onClick={save}>
                    Save
                  </Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </>
      )}
    </>
  );
};
