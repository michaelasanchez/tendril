import type { ReactNode } from 'react';
import styles from './Badge.module.css';

interface Props {
  children?: ReactNode;
}

export const Badge: React.FC<Props> = ({ children }) => {
  return <div className={styles.Badge}>{children}</div>
}