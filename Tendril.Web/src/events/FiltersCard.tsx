import cn from 'classnames';
import { Card } from 'react-bootstrap';
import { Button } from '../components/button';
import { FormInput } from '../components/form';
import { Icon } from '../components/Icon';
import cardStyles from '../styles/Card.module.css';
import styles from './FiltersCard.module.css';

export interface EventFilter {
  title?: string;
  startDate?: string;
  endDate?: string;
  favoritesOnly?: boolean;
  category?: string | null;
  location?: string | null;
}

interface Props {
  className?: string;
  filter: EventFilter;
  locations: string[];
  onChange: (update: Partial<EventFilter>) => void;
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

export const FiltersCard: React.FC<Props> = ({
  className,
  filter,
  locations,
  onChange,
}) => {
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

        <div
          className={styles.Checkbox}
          onClick={() => onChange({ favoritesOnly: !filter.favoritesOnly })}
        >
          <input
            className="form-check-input"
            type="checkbox"
            checked={filter.favoritesOnly ?? false}
            onChange={(e) => {
              e.stopPropagation();
              onChange({ favoritesOnly: !filter.favoritesOnly });
            }}
          />
          <label>SHOW FAVORITES ONLY</label>
        </div>

        <label>CATEGORY</label>
        <div className={styles.ButtonGroup}>
          <Button
            variant={!filter.category ? 'active' : 'default'}
            onClick={() => onChange({ category: null })}
          >
            All
          </Button>

          {categories.map((c, i) => (
            <Button
              key={i}
              variant={filter.category === c ? 'active' : 'default'}
              onClick={() => onChange({ category: c })}
            >
              {c}
            </Button>
          ))}
        </div>

        <label>LOCATION</label>
        <div className={styles.ButtonGroup}>
          <Button
            variant={!filter.location ? 'active' : 'default'}
            onClick={() => onChange({ location: null })}
          >
            All Locations
          </Button>

          {locations.map((l, i) => (
            <Button
              key={i}
              variant={filter.location === l ? 'active' : 'default'}
              onClick={() => onChange({ location: l })}
            >
              {l}
            </Button>
          ))}
        </div>
      </Card.Body>
    </Card>
  );
};
