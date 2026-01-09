import styles from './Button.module.css';

interface Props {
  children?: React.ReactNode;
}

export const ButtonGroup: React.FC<Props> = ({ children }) => {
  return <div className={styles.ButtonGroup}>{children}</div>;
};
