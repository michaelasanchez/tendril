import cn from 'classnames';
import React from 'react';
import styles from './Button.module.css';
import { Button, type ButtonProps } from './Button.tsx';

export interface SquareButtonProps extends ButtonProps {}

export const SquareButton: React.FC<SquareButtonProps> = (props) => {
  const { className, ...rest } = props;

  return <Button className={cn(styles.SquareButton, className)} {...rest} />;
};
