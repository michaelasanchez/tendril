import cn from 'classnames';
import type { ReactNode } from 'react';
import styles from './Badge.module.css';

interface Props {
  children?: ReactNode;
  className?: string;
}

export const Badge: React.FC<Props> = ({ children, className }) => {
  return <div className={cn(styles.Badge, className)}>{children}</div>;
};
