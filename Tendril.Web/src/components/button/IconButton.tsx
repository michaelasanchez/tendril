import cn from 'classnames';
import React from 'react';
import { Icon, type IconName } from '../Icon.tsx';
import styles from './Button.module.css';
import { Button, type ButtonProps } from './Button.tsx';

export interface IconButtonProps extends ButtonProps {
  name: IconName;
}

export const IconButton: React.FC<IconButtonProps> = (props) => {
  const { className, children, name, ...rest } = props;

  return (
    <Button className={cn(styles.IconButton, className)} {...rest}>
      <Icon name={name} />
      {children}
    </Button>
  );
};
