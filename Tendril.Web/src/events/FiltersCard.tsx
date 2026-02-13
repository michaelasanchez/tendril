import cn from 'classnames';
import { useState } from 'react';
import { Card } from 'react-bootstrap';
import type { EventFilter } from '../api/events';
import { Button } from '../components/button';
import { FormInput } from '../components/form';
import { Icon } from '../components/Icon';
import cardStyles from '../styles/Card.module.css';
import type { Venue } from '../types/api';
import styles from './FiltersCard.module.css';

interface Props {
  className?: string;
  favoritesOnly?: boolean;
  filter: EventFilter;
  venues: Venue[];
  onChange: (update: Partial<EventFilter>) => void;
  onToggleFavoritesOnly: () => void;
}

const categories = [
  'Concert',
  'Comedy',
  'Sports',
  'Art',
  'Theater',
  'Food & Drink',
  'Other',
];

const numCategories = 4;
const numVenues = 4;

export const FiltersCard: React.FC<Props> = ({
  className,
  favoritesOnly,
  filter,
  venues,
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
          label="SEARCH EVENTS"
          value={filter.title ?? ''}
          placeholder="Search by event name..."
          onChange={(title) => onChange({ title })}
        />
        <FormInput
          label="DATE RANGE"
          type="date"
          value={filter.startDate ?? ''}
          placeholder="mm/dd/yyyy"
          onChange={(startDate) => onChange({ startDate })}
        />
        <FormInput
          className={styles.NoLabel}
          type="date"
          value={filter.endDate ?? ''}
          placeholder="mm/dd/yyyy"
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
            variant={!filter.categories?.length ? 'active' : 'default'}
            onClick={() => onChange({ categories: [] })}
          >
            All
          </Button>

          {categories
            .slice(0, showMoreCategories ? categories.length : numCategories)
            .map((c, i) => {
              const active = filter.categories?.includes(c);

              return (
                <Button
                  key={i}
                  variant={active ? 'active' : 'default'}
                  onClick={() =>
                    onChange({
                      categories: active
                        ? filter.categories?.filter((cat) => cat !== c)
                        : [...(filter.categories ?? []), c],
                    })
                  }
                >
                  {c}
                </Button>
              );
            })}

          <Button
            variant="default"
            onClick={() => setMoreCategories(!showMoreCategories)}
          >
            {showMoreCategories
              ? 'Show less'
              : `+ ${categories.length - numCategories} More`}{' '}
            <Icon name={showMoreCategories ? 'up' : 'down'} />
          </Button>
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
            .slice(0, showMoreVenues ? venues.length : numVenues)
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

          <Button
            variant="default"
            onClick={() => setShowMoreVenues(!showMoreVenues)}
          >
            {showMoreVenues
              ? 'Show less'
              : `+ ${venues.length - numVenues} More`}{' '}
            <Icon name={showMoreVenues ? 'up' : 'down'} />
          </Button>
        </div>
      </Card.Body>
    </Card>
  );
};
