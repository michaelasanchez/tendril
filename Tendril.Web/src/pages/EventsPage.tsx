// src/pages/EventsPage.tsx
import React, { useEffect, useState } from "react";
import { EventsApi } from "../api/events";
import type { Event } from "../types/api";

export const EventsPage: React.FC = () => {
  const [events, setEvents] = useState<Event[]>([]);

  useEffect(() => {
    void (async () => {
      const data = await EventsApi.getAll();
      setEvents(data);
    })();
  }, []);

  return (
    <section>
      <div className="page-header">
        <h2>Events</h2>
      </div>

      <table className="data-table">
        <thead>
          <tr>
            <th>Title</th>
            <th>Venue</th>
            <th>Start</th>
            <th>End</th>
            <th>Ticket URL</th>
          </tr>
        </thead>
        <tbody>
          {events.map(e => (
            <tr key={e.id}>
              <td>{e.title}</td>
              <td>{e.venue?.name ?? e.venueId}</td>
              <td>{e.startUtc}</td>
              <td>{e.endUtc ?? "-"}</td>
              <td>
                {e.ticketUrl ? (
                  <a href={e.ticketUrl} target="_blank" rel="noreferrer">
                    Link
                  </a>
                ) : (
                  "-"
                )}
              </td>
            </tr>
          ))}
          {events.length === 0 && (
            <tr>
              <td colSpan={5}>No events.</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  );
};
