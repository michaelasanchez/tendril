import {
  Archive,
  ArchiveRestore,
  ArrowLeft,
  ArrowRight,
  Calendar,
  ChevronDown,
  ChevronUp,
  CloudDownload,
  CloudUpload,
  Copy,
  Edit,
  ExternalLink,
  Eye,
  EyeOff,
  Flag,
  FlagOff,
  Funnel,
  Heart,
  LogIn,
  LogOut,
  MapPin,
  Moon,
  Play,
  Plus,
  Share2,
  SlidersHorizontal,
  Sun,
  Ticket,
  Trash,
  X,
  Zap,
  ZapOff
} from 'lucide-react';

const Icons = {
  archive: Archive,
  calendar: Calendar,
  close: X,
  copy: Copy,
  create: Plus,
  dark: Moon,
  disabled: ZapOff,
  down: ChevronDown,
  edit: Edit,
  enable: Zap,
  external: ExternalLink,
  favorite: Heart,
  filter: Funnel,
  flag: Flag,
  flagOff: FlagOff,
  invisible: EyeOff,
  light: Sun,
  location: MapPin,
  login: LogIn,
  logout: LogOut,
  next: ArrowRight,
  previous: ArrowLeft,
  publish: CloudUpload,
  remove: Trash,
  run: Play,
  share: Share2,
  sliders: SlidersHorizontal,
  ticket: Ticket,
  unarchive: ArchiveRestore,
  unpublish: CloudDownload,
  up: ChevronUp,
  visible: Eye,
};

export type IconName = keyof typeof Icons;

interface IconProps {
  name: IconName;
  size?: number;
  color?: string;
}

export const Icon: React.FC<IconProps> = ({
  name,
  size = 16,
  color = 'currentColor',
}) => {
  const IconComponent = Icons[name];
  return IconComponent ? <IconComponent size={size} color={color} /> : null;
};
