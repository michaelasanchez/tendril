import cn from 'classnames';
import { Navbar as BootstrapNavbar, Container, Nav } from 'react-bootstrap';
import type { AppTheme } from '../../hooks';
import { cardStyles } from '../../styles';
import { SquareButton } from '../button';
import { Icon } from '../Icon';
import styles from './Navbar.module.css';
interface Props {
  authorized: boolean;
  theme: AppTheme;
  onLogout: () => void;
  onThemeToggle: () => void;
}

export const Navbar: React.FC<Props> = ({
  authorized,
  theme,
  onLogout,
  onThemeToggle,
}) => {
  return (
    <BootstrapNavbar className={styles.Navbar} expand={false}>
      <Container>
        <div className={styles.Group}>
          <div className={styles.Logo}>
            <Icon name="calendar" size={24} />
          </div>
          <div className={styles.Brand}>
            <h3>Hello Local</h3>
            <div className={styles.Caption}>Find. Share. Enjoy.</div>
          </div>
        </div>
        <div className={styles.Group}>
          <div className={cn(cardStyles.BgCard, styles.Location)}>
            Grand Rapids, MI
          </div>
          <div className="d-none d-sm-block">
            <SquareButton onClick={onThemeToggle}>
              <Icon name={theme} />
            </SquareButton>
          </div>
          {authorized && <BootstrapNavbar.Toggle />}
        </div>
        <BootstrapNavbar.Collapse>
          <Nav className={styles.NavRight}>
            <Nav.Link href="/">Events</Nav.Link>
            <hr />
            <Nav.Link href="/admin/scrapers">Scrapers</Nav.Link>
            <Nav.Link href="/admin/categories">Categories</Nav.Link>
            <Nav.Link href="/admin/tags">Tags</Nav.Link>
            <Nav.Link href="/admin/venues">Venues</Nav.Link>
            <Nav.Link href="/admin/summary">Summary</Nav.Link>
            <hr />
            <Nav.Link onClick={onLogout}>
              Logout
            </Nav.Link>
          </Nav>
        </BootstrapNavbar.Collapse>
      </Container>
    </BootstrapNavbar>
  );
};
