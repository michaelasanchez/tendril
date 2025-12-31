import cn from "classnames";
import { Card } from "react-bootstrap";
import { FormInput } from "../components/form";
import { Icon } from "../components/Icon";
import buttonStyles from "../styles/Button.module.css";
import cardStyles from "../styles/Card.module.css";
import styles from "./FiltersCard.module.css";

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
  "Concert",
  "Comedy",
  "Sports",
  "Art",
  "Theater",
  "Food & Drink",
  "Other",
];

export const FiltersCard: React.FC<Props> = ({
  className,
  filter,
  locations,
  onChange,
}) => {
  return (
    <Card className={cn(styles.FiltersCard, cardStyles.BgCard, className)}>
      <Card.Body className={cardStyles.CardBody}>
        <h4>
          <Icon name="filter" />
          Filters
        </h4>
        <FormInput
          label="SEARCH EVENTS"
          value={filter.title ?? ""}
          placeholder="Search by event name..."
          onChange={(title) => onChange({ title })}
        />
        <FormInput
          label="DATE RANGE"
          value={filter.startDate ?? ""}
          placeholder="yyyy-mm-dd"
          onChange={(startDate) => onChange({ startDate })}
        />
        <FormInput
          className={styles.NoLabel}
          value={filter.endDate ?? ""}
          placeholder="yyyy-mm-dd"
          onChange={(endDate) => onChange({ endDate })}
        />


        <label>CATEGORY</label>
        <div className={styles.ButtonGroup}>
          <button
            className={cn(
              buttonStyles.Button,
              !filter.category && buttonStyles.Active
            )}
            onClick={() => onChange({ category: null })}
          >
            All
          </button>

          {categories.map((c, i) => (
            <button
              key={i}
              className={cn(
                buttonStyles.Button,
                filter.category === c && buttonStyles.Active
              )}
              onClick={() => onChange({ category: c })}
            >
              {c}
            </button>
          ))}
        </div>


        <label>LOCATION</label>
        <div className={styles.ButtonGroup}>
          <button
            className={cn(
              buttonStyles.Button,
              !filter.location && buttonStyles.Active
            )}
            onClick={() => onChange({ location: null })}
          >
            All Locations
          </button>

          {locations.map((l, i) => (
            <button
              key={i}
              className={cn(
                buttonStyles.Button,
                filter.location === l && buttonStyles.Active
              )}
              onClick={() => onChange({ location: l })}
            >
              {l}
            </button>
          ))}
        </div>
      </Card.Body>
    </Card>
  );
};
