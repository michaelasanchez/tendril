import cn from 'classnames';
import { Navbar as BootstrapNavbar, Container } from 'react-bootstrap';
import type { AppTheme } from '../../hooks';
import { cardStyles } from '../../styles';
import { SquareButton } from '../button';
import { Icon } from '../Icon';
import styles from './Navbar.module.css';
interface Props {
  theme: AppTheme;
  onThemeToggle: () => void;
}

export const Navbar: React.FC<Props> = ({ theme, onThemeToggle }) => {
  return (
    <BootstrapNavbar className={styles.Navbar}>
      <Container>
        <div className={styles.Group}>
          <div className={styles.Logo}>
            <Icon name="calendar" size={24} />
          </div>
          <div className={styles.Brand}>
            <h3>Local Events</h3>
            <caption>Discover what's happening in your city</caption>
          </div>
        </div>
        <div className={styles.Group}>
          <div className={cn(cardStyles.BgCard, styles.Location)}>
            Grand Rapids, MI
          </div>
          <SquareButton onClick={onThemeToggle}>
            <Icon name={theme} />
          </SquareButton>
        </div>
      </Container>
    </BootstrapNavbar>
  );
};
