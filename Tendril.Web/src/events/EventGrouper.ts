import { format, parse } from 'date-fns';
import type { Event } from '../types/api';

export interface EventGroup {
  label: string;
  events: Event[];
}

function groupEventsByDay(events: Event[]): EventGroup[] {
  const grouped = events.reduce((groups, event) => {
    const dateKey = event.startDateTime?.split('T')[0] ?? event.startDate!;
    if (!groups[dateKey]) {
      groups[dateKey] = [];
    }

    groups[dateKey].push(event);

    return groups;
  }, {} as Record<string, Event[]>);

  return Object.keys(grouped)
    .sort()
    .map((g) => {
      const dateObj = parse(g, 'yyyy-MM-dd', new Date());

      return {
        label: format(dateObj, 'EEEE, MMM dd'),
        events: grouped[g],
      };
    });
}

export const EventGrouper = {
  byDay: groupEventsByDay,
};