import moment from "moment";
import React, { useState } from "react";
import type { Event as CalendarEvent, View } from "react-big-calendar";
import { Calendar, momentLocalizer } from "react-big-calendar";

const localizer = momentLocalizer(moment);

interface EventCalendarProps {
  events: CalendarEvent[];
}

export const EventCalendar: React.FC<EventCalendarProps> = ({ events }) => {
  const [date, setDate] = useState<Date>(new Date());
  const [view, setView] = useState<View>("month");

  return (
    <Calendar
      localizer={localizer}
      events={events}
      startAccessor="start"
      endAccessor="end"
      views={["month", "agenda"]}
      view={view}
      date={date}
      onNavigate={(date, view) => {
        setDate(date);
        setView(view);
      }}
      onView={setView}
      style={{ height: 800, width: "100vw" }}
    />
  );
};
