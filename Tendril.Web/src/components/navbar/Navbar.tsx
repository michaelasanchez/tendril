import cn from 'classnames';
import { useState } from 'react';
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

const test = [
  // 'Built for locals, by locals',
  'Find. Share. Enjoy.',
  'Your neighborhood, all in one place',
  // 'Everything, everywhere, locally',
  // 'Find. Share. Go.',
];

export const Navbar: React.FC<Props> = ({ theme, onThemeToggle }) => {
  const [b, setB] = useState<number>(0);
  return (
    <BootstrapNavbar className={styles.Navbar}>
      <Container>
        <div className={styles.Group}>
          <div className={styles.Logo}>
            <Icon name="calendar" size={24} />
          </div>
          <div className={styles.Brand}>
            <h3>Hello Local</h3>
            <div
              onClick={() => setB((l) => (l + 1) % test.length)}
              className={styles.Caption}
            >
              {test[b]}
            </div>
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
