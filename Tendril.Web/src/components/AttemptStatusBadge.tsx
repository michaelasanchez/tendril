import { Badge } from 'react-bootstrap';
import type { AttemptStatus } from '../types/api';

export const AttemptStatusBadge = ({ status }: { status: AttemptStatus }) => (
  <Badge
    bg={
      status === 'Succeeded'
        ? 'success'
        : status === 'Failed'
          ? 'danger'
          : 'warning'
    }
    text={status === 'Succeeded' || status === 'Failed' ? 'light' : 'dark'}
  >
    {status === 'Running'
      ? ' RUNNING'
      : status === 'Succeeded'
        ? 'SUCCESS'
        : status === 'Warning'
          ? 'WARNING'
          : 'FAILED'}
  </Badge>
);
