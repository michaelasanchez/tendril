import { useState, useMemo } from 'react';
import { startOfDay } from 'date-fns';
import type { Event } from '../types/api';

export interface ShowStatus {
  past: boolean;
  pending: boolean;
  published: boolean;
  suppressed: boolean;
}

export interface ShowCategory {
  [category: string]: boolean;
}

export interface FilterState {
  status: ShowStatus;
  category: ShowCategory;
}

export interface FilterStats {
  past: number;
  pending: number;
  published: number;
  suppressed: number;
  categories: { [category: string]: number };
}

const today = startOfDay(new Date());

export const useEventFilter = (events: Event[]) => {
  const [filter, setFilter] = useState<FilterState>({
    status: { past: false, pending: true, published: true, suppressed: false },
    category: {},
  });

  // 1. Calculate Statistics dynamically based on incoming events
  const stats = useMemo(() => {
    const s: FilterStats = {
      past: 0,
      pending: 0,
      published: 0,
      suppressed: 0,
      categories: {},
    };

    events.forEach((e) => {
      const eventDate = startOfDay(new Date(e.startDateTime ?? e.startDate!));
      if (eventDate <= today) s.past++;

      if (e.status === 'Pending') s.pending++;
      if (e.status === 'Published') s.published++;
      if (e.status === 'Suppressed') s.suppressed++;

      const cat = e.categoryName ?? 'None';
      s.categories[cat] = (s.categories[cat] || 0) + 1;
    });
    return s;
  }, [events]);

  // 2. Perform Filtering
  const filteredEvents = useMemo(() => {
    return events.filter((e) => {
      const eventDate = startOfDay(new Date(e.startDateTime ?? e.startDate!));

      if (!filter.status.past && eventDate < today) return false;
      if (!filter.status.pending && e.status === 'Pending') return false;
      if (!filter.status.published && e.status === 'Published') return false;
      if (!filter.status.suppressed && e.status === 'Suppressed') return false;

      const cat = e.categoryName ?? 'None';
      // If categories are initialized in filter and set to false, filter it out
      if (filter.category[cat] === false) return false;

      return true;
    });
  }, [events, filter]);

  const toggleStatus = (key: keyof ShowStatus) => {
    setFilter((prev) => ({
      ...prev,
      status: { ...prev.status, [key]: !prev.status[key] },
    }));
  };

  const toggleCategory = (category: string) => {
    setFilter((prev) => ({
      ...prev,
      category: {
        ...prev.category,
        // Defaulting to true if undefined, then flipping
        [category]: prev.category[category] === false,
      },
    }));
  };

  return {
    filter,
    filteredEvents,
    stats,
    toggleStatus,
    toggleCategory,
  };
};
