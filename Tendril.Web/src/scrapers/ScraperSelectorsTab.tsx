// src/scrapers/ScraperSelectorsTab.tsx
import React, { useState } from "react";
import { Card, Form, Table } from "react-bootstrap";
import { ScrapersApi } from "../api/scrapers";
import { FormCheck, FormInput, FormSelect } from "../components/form";
import formStyles from "../components/form/Form.module.css";
import type { Guid, ScraperSelector, SelectorType } from "../types/api";

interface Props {
  scraperId: Guid;
  selectors: ScraperSelector[];
  refresh: () => Promise<void>;
}

const selectorTypeOptions = [
  "Container",
  "Text",
  "Attribute",
  "Href",
  "Src",
  "Click",
  "Hover",
  "Scroll",
].map((type) => ({ value: type, label: type }));

export const ScraperSelectorsTab: React.FC<Props> = ({
  scraperId,
  selectors,
  refresh: load,
}) => {
  const [editing, setEditing] = useState<Partial<ScraperSelector>>({});
  const [isNew, setIsNew] = useState(false);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      fieldName: "",
      selector: "",
      order: selectors.length,
      root: false,
      type: "Text",
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
        selector: editing.selector ?? "",
        order: editing.order ?? selectors.length,
        root: editing.root ?? false,
        type: editing.type,
        attribute: Boolean(editing.attribute)
          ? editing.attribute ?? null
          : null,
        delay: editing.delay ?? null,
      });
    } else if (editing.id) {
      await ScrapersApi.updateSelector(scraperId, editing.id, {
        fieldName: editing.fieldName,
        selector: editing.selector,
        order: editing.order,
        root: editing.root,
        type: editing.type,
        attribute: Boolean(editing.attribute)
          ? editing.attribute ?? null
          : null,
        delay: editing.delay ?? null,
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
    <Card>
      <Card.Body>
        <div style={{ display: "flex", justifyContent: "space-between" }}>
          <h3>Selectors</h3>
          <button onClick={startNew}>Add Selector</button>
        </div>
      </Card.Body>
      <Card.Body>
        <Table className="data-table" hover>
          <thead>
            <tr>
              <th>Field</th>
              <th>Selector</th>
              <th>Order</th>
              <th>Root</th>
              <th>Type</th>
              <th>Attribute</th>
              <th>Delay</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {selectors
              .sort((a, b) => a.order - b.order)
              .map((s) => (
                <tr key={s.id}>
                  <td>{s.fieldName}</td>
                  <td>
                    <code>{s.selector}</code>
                  </td>
                  <td>{s.order}</td>
                  <td>{s.root ? "Yes" : ""}</td>
                  <td>{s.type}</td>
                  <td>{s.attribute}</td>
                  <td>{s.delay}</td>
                  <td>
                    <button onClick={() => startEdit(s)}>Edit</button>
                    <button onClick={() => remove(s)}>Delete</button>
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

        {editing.fieldName !== undefined && (
          <div className="edit-panel">
            <h4>{isNew ? "New Selector" : "Edit Selector"}</h4>
            <Form className={formStyles.form}>
              <FormInput
                label="Field Name"
                value={editing.fieldName ?? ""}
                onChange={(fieldName) => setEditing({ ...editing, fieldName })}
              />
              <FormInput
                label="Selector"
                value={editing.selector ?? ""}
                onChange={(selector) => setEditing({ ...editing, selector })}
              />
              <FormInput
                type="number"
                label="Order"
                value={editing.order?.toString() ?? "0"}
                onChange={(order) =>
                  setEditing({ ...editing, order: parseInt(order) })
                }
              />
              <FormCheck
                label="Root"
                checked={editing.root ?? false}
                onChange={(checked) =>
                  setEditing({ ...editing, root: checked })
                }
              />
              <FormSelect
                label="Type"
                value={editing.type ?? "Text"}
                onChange={(value) =>
                  setEditing({ ...editing, type: value as SelectorType })
                }
                options={selectorTypeOptions}
              />
              {editing.type === "Attribute" && (
                <FormInput
                  label="Attribute"
                  value={editing.attribute ?? ""}
                  onChange={(attribute) =>
                    setEditing({ ...editing, attribute })
                  }
                />
              )}
              <FormInput
                type="number"
                label="Delay"
                value={editing.delay?.toString() ?? ""}
                onChange={(delay) =>
                  setEditing({ ...editing, delay: parseInt(delay) })
                }
              />
              <div className={formStyles.buttonRow}>
                <button onClick={save}>Save</button>
                <button onClick={cancelEdit}>Cancel</button>
              </div>
            </Form>
          </div>
        )}
      </Card.Body>
    </Card>
  );
};
