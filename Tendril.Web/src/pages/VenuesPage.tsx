// src/pages/VenuesPage.tsx
import React, { useEffect, useState } from "react";
import { VenuesApi } from "../api/venues";
import type { Venue } from "../types/api";

export const VenuesPage: React.FC = () => {
  const [venues, setVenues] = useState<Venue[]>([]);
  const [editing, setEditing] = useState<Partial<Venue>>({});
  const [isNew, setIsNew] = useState(false);

  const load = async () => {
    const data = await VenuesApi.getAll();
    setVenues(data);
  };

  useEffect(() => {
    void load();
  }, []);

  const startNew = () => {
    setIsNew(true);
    setEditing({ name: "", address: "", website: "" });
  };

  const startEdit = (v: Venue) => {
    setIsNew(false);
    setEditing({ ...v });
  };

  const cancel = () => {
    setEditing({});
    setIsNew(false);
  };

  const save = async () => {
    if (!editing.name || !editing.address) return;
    if (isNew) {
      await VenuesApi.create({
        name: editing.name,
        address: editing.address,
        website: editing.website
      });
    } else if (editing.id) {
      await VenuesApi.update(editing.id, {
        name: editing.name,
        address: editing.address,
        website: editing.website
      });
    }
    await load();
    cancel();
  };

  const remove = async (v: Venue) => {
    if (!window.confirm(`Delete venue "${v.name}"?`)) return;
    await VenuesApi.delete(v.id);
    await load();
  };

  return (
    <section>
      <div className="page-header">
        <h2>Venues</h2>
        <button onClick={startNew}>New Venue</button>
      </div>

      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Address</th>
            <th>Website</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {venues.map(v => (
            <tr key={v.id}>
              <td>{v.name}</td>
              <td>{v.address}</td>
              <td>
                {v.website ? (
                  <a href={v.website} target="_blank" rel="noreferrer">
                    {v.website}
                  </a>
                ) : (
                  "-"
                )}
              </td>
              <td>
                <button onClick={() => startEdit(v)}>Edit</button>
                <button onClick={() => remove(v)}>Delete</button>
              </td>
            </tr>
          ))}
          {venues.length === 0 && (
            <tr>
              <td colSpan={4}>No venues yet.</td>
            </tr>
          )}
        </tbody>
      </table>

      {(editing.name !== undefined) && (
        <div className="card edit-panel">
          <h3>{isNew ? "New Venue" : "Edit Venue"}</h3>
          <div className="form-grid">
            <label>
              Name
              <input
                value={editing.name ?? ""}
                onChange={e => setEditing({ ...editing, name: e.target.value })}
              />
            </label>
            <label>
              Address
              <input
                value={editing.address ?? ""}
                onChange={e => setEditing({ ...editing, address: e.target.value })}
              />
            </label>
            <label>
              Website
              <input
                value={editing.website ?? ""}
                onChange={e => setEditing({ ...editing, website: e.target.value })}
              />
            </label>
          </div>
          <div className="button-row">
            <button onClick={save}>Save</button>
            <button onClick={cancel}>Cancel</button>
          </div>
        </div>
      )}
    </section>
  );
};
