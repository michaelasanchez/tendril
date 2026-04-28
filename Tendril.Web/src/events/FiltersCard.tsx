import cn from 'classnames';
import { useState } from 'react';
import { Card } from 'react-bootstrap';
import type { EventFilter } from '../api/events';
import { Button } from '../components/button';
import { FormDate, FormInput } from '../components/form';
import { Icon } from '../components/Icon';
import cardStyles from '../styles/Card.module.css';
import type { Category, Venue } from '../types/api';
import styles from './FiltersCard.module.css';

interface Props {
  className?: string;
  favoritesOnly?: boolean;
  filter: EventFilter;
  categories: Category[];
  venues: Venue[];
  // available: AvailableFilters;
  onChange: (update: Partial<EventFilter>) => void;
  onToggleFavoritesOnly: () => void;
}

const categoryShowCount = 4;
const venueShowCount = 4;

// export type AvailableFilters = Pick<EventResponse, 'categoryIds' | 'venueIds'>;

export const FiltersCard: React.FC<Props> = ({
  className,
  favoritesOnly,
  filter,
  categories,
  venues,
  // available,
  onChange,
  onToggleFavoritesOnly,
}) => {
  const [showMoreCategories, setMoreCategories] = useState<boolean>(false);
  const [showMoreVenues, setShowMoreVenues] = useState<boolean>(false);

  return (
    <Card className={cn(cardStyles.BgCard, styles.FiltersCard, className)}>
      <Card.Body className={cardStyles.CardBody}>
        <h4>
          <Icon name="filter" />
          Filters
        </h4>
        <FormInput
          label="Search Events"
          value={filter.title ?? ''}
          placeholder="Search by event name..."
          clearable
          onChange={(title) => onChange({ title })}
        />
        <FormDate
          label="Date Range"
          value={filter.startDate ?? ''}
          onChange={(startDate) => onChange({ startDate })}
        />
        <FormDate
          className={styles.NoLabel}
          value={filter.endDate ?? ''}
          onChange={(endDate) => onChange({ endDate })}
        />
        <div className={styles.Checkbox} onClick={onToggleFavoritesOnly}>
          <input
            className="form-check-input"
            type="checkbox"
            checked={favoritesOnly ?? false}
            onChange={(e) => {
              e.stopPropagation();
            }}
          />
          <label>Show Favorites Only</label>
        </div>
        <label>Category</label>
        <div className={styles.ButtonGroup}>
          <Button
            variant={!filter.categoryIds?.length ? 'active' : 'default'}
            onClick={() => onChange({ categoryIds: [] })}
          >
            All
          </Button>

          {categories
            .filter(
              (c, i) =>
                showMoreCategories ||
                i < categoryShowCount ||
                filter.categoryIds?.includes(c.id),
            )
            // .slice(
            //   0,
            //   showMoreCategories ? categories.length : categoryShowCount,
            // )
            .map((c, i) => {
              const active = filter.categoryIds?.includes(c.id);

              return (
                <Button
                  key={i}
                  variant={active ? 'active' : 'default'}
                  onClick={() =>
                    onChange({
                      categoryIds: active
                        ? (filter.categoryIds?.filter((id) => id !== c.id) ??
                          [])
                        : [...(filter.categoryIds ?? []), c.id],
                    })
                  }
                >
                  {c.name}
                </Button>
              );
            })}

          {categories.length > categoryShowCount && (
            <Button
              variant="default"
              onClick={() => setMoreCategories(!showMoreCategories)}
            >
              {showMoreCategories
                ? 'Show less'
                : `+ ${categories.length - categoryShowCount} More`}{' '}
              <Icon name={showMoreCategories ? 'up' : 'down'} />
            </Button>
          )}
        </div>
        <label>Location</label>
        <div className={styles.ButtonGroup}>
          <Button
            variant={!filter.venueIds?.length ? 'active' : 'default'}
            onClick={() => onChange({ venueIds: [] })}
          >
            All Locations
          </Button>

          {venues
            .slice(0, showMoreVenues ? venues.length : venueShowCount)
            .map((v, i) => {
              const active = filter.venueIds?.includes(v.id);
              return (
                <Button
                  key={i}
                  variant={active ? 'active' : 'default'}
                  onClick={() =>
                    onChange({
                      venueIds: active
                        ? (filter.venueIds?.filter((id) => id !== v.id) ?? [])
                        : [...(filter.venueIds ?? []), v.id],
                    })
                  }
                >
                  {v.name}
                </Button>
              );
            })}

          {venues.length > venueShowCount && (
            <Button
              variant="default"
              onClick={() => setShowMoreVenues(!showMoreVenues)}
            >
              {showMoreVenues
                ? 'Show less'
                : `+ ${venues.length - venueShowCount} More`}{' '}
              <Icon name={showMoreVenues ? 'up' : 'down'} />
            </Button>
          )}
        </div>
      </Card.Body>
    </Card>
  );
};
