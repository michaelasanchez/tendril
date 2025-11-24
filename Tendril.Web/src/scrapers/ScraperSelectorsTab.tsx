// src/scrapers/ScraperSelectorsTab.tsx
import React, { useEffect, useState } from "react";
import type { Guid, ScraperSelector, SelectorType } from "../types/api";
import { ScrapersApi } from "../api/scrapers";

interface Props {
  scraperId: Guid;
}

const selectorTypeOptions: SelectorType[] = ["Css", "XPath", "Regex"];

export const ScraperSelectorsTab: React.FC<Props> = ({ scraperId }) => {
  const [selectors, setSelectors] = useState<ScraperSelector[]>([]);
  const [editing, setEditing] = useState<Partial<ScraperSelector>>({});
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const data = await ScrapersApi.getSelectors(scraperId);
    setSelectors(data);
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

  const startNew = () => {
    setIsNew(true);
    setEditing({
      fieldName: "",
      selector: "",
      selectorType: "Css",
      outer: false
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
    if (!editing.fieldName || !editing.selector || !editing.selectorType) return;

    if (isNew) {
      await ScrapersApi.createSelector(scraperId, {
        fieldName: editing.fieldName,
        selector: editing.selector,
        selectorType: editing.selectorType,
        outer: editing.outer ?? false
      });
    } else if (editing.id) {
      await ScrapersApi.updateSelector(scraperId, editing.id, {
        fieldName: editing.fieldName,
        selector: editing.selector,
        selectorType: editing.selectorType,
        outer: editing.outer ?? false
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
            <th>Type</th>
            <th>Outer?</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {selectors.map(s => (
            <tr key={s.id}>
              <td>{s.fieldName}</td>
              <td><code>{s.selector}</code></td>
              <td>{s.selectorType}</td>
              <td>{s.outer ? "Yes" : "No"}</td>
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

      {(editing.fieldName !== undefined) && (
        <div className="edit-panel">
          <h4>{isNew ? "New Selector" : "Edit Selector"}</h4>
          <div className="form-grid">
            <label>
              Field Name
              <input
                value={editing.fieldName ?? ""}
                onChange={e => setEditing({ ...editing, fieldName: e.target.value })}
              />
            </label>
            <label>
              Selector
              <input
                value={editing.selector ?? ""}
                onChange={e => setEditing({ ...editing, selector: e.target.value })}
              />
            </label>
            <label>
              Selector Type
              <select
                value={editing.selectorType ?? "Css"}
                onChange={e =>
                  setEditing({ ...editing, selectorType: e.target.value as SelectorType })
                }
              >
                {selectorTypeOptions.map(st => (
                  <option key={st} value={st}>{st}</option>
                ))}
              </select>
            </label>
            <label>
              Outer (list item)?
              <input
                type="checkbox"
                checked={editing.outer ?? false}
                onChange={e => setEditing({ ...editing, outer: e.target.checked })}
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
