import cn from 'classnames';
import React from 'react';
import { buttonStyles } from '../../styles/index.ts';
import { Button, type ButtonProps } from './Button.tsx';

export interface SquareButtonProps extends ButtonProps {}

export const SquareButton: React.FC<SquareButtonProps> = (props) => {
  const { className, ...rest } = props;

  return (
    <Button className={cn(buttonStyles.SquareButton, className)} {...rest} />
  );
};
