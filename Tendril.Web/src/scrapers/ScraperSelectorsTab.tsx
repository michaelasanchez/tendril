// src/scrapers/ScraperSelectorsTab.tsx
import React, { useState } from "react";
import { ScrapersApi } from "../api/scrapers";
import type { Guid, ScraperSelector, SelectorType } from "../types/api";

interface Props {
  scraperId: Guid;
  selectors: ScraperSelector[];
  refresh: () => Promise<void>;
}

const selectorTypeOptions: SelectorType[] = [
  "Container",
  "Text",
  "Attribute",
  "Href",
  "Src",
  "Click",
  "Hover",
  "Scroll"
];

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
        attribute: Boolean(editing.attribute) ? editing.attribute ?? null : null,
        delay: editing.delay ?? null,
      });
    } else if (editing.id) {
      await ScrapersApi.updateSelector(scraperId, editing.id, {
        fieldName: editing.fieldName,
        selector: editing.selector,
        order: editing.order,
        root: editing.root,
        type: editing.type,
        attribute: Boolean(editing.attribute) ? editing.attribute ?? null : null,
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
    <div className="card">
      <div className="card-header">
        <h3>Selectors</h3>
        <button onClick={startNew}>Add Selector</button>
      </div>

      <table className="data-table">
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
      </table>

      {editing.fieldName !== undefined && (
        <div className="edit-panel">
          <h4>{isNew ? "New Selector" : "Edit Selector"}</h4>
          <div className="form-grid">
            <label>
              Field Name
              <input
                value={editing.fieldName ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, fieldName: e.target.value })
                }
              />
            </label>
            <label>
              Selector
              <input
                value={editing.selector ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, selector: e.target.value })
                }
              />
            </label>
            <label>
              Order
              <input
                type="number"
                value={editing.order ?? 0}
                onChange={(e) =>
                  setEditing({ ...editing, order: e.target.valueAsNumber })
                }
              />
            </label>
            <label>
              Root
              <input
                type="checkbox"
                checked={editing.root ?? false}
                onChange={(e) =>
                  setEditing({ ...editing, root: e.target.checked })
                }
              />
            </label>
            <label>
              Type
              <select
                value={editing.type ?? "Text"}
                onChange={(e) =>
                  setEditing({
                    ...editing,
                    type: e.target.value as SelectorType,
                  })
                }
              >
                {selectorTypeOptions.map((st) => (
                  <option key={st} value={st}>
                    {st}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Attribute
              <input
                value={editing.attribute ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, attribute: e.target.value })
                }
              />
            </label>
            <label>
              Delay
              <input
                type="number"
                value={editing.delay ?? ""}
                onChange={(e) =>
                  setEditing({ ...editing, delay: e.target.valueAsNumber })
                }
              />
            </label>
          </div>
          <div className="button-row">
            <button onClick={save}>Save</button>
            <button onClick={cancelEdit}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
};
