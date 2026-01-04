import cn from 'classnames';
import React from 'react';
import { buttonStyles } from '../../styles';
import { Button, type ButtonProps } from './Button.tsx';

export interface AdminButtonProps extends ButtonProps {}

export const AdminButton: React.FC<AdminButtonProps> = (props) => {
  const { className, ...rest } = props;

  return (
    <Button className={cn(buttonStyles.AdminButton, className)} {...rest} />
  );
};
