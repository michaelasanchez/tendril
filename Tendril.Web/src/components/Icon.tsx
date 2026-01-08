import { Calendar, ExternalLink, Funnel, Heart, MapPin, Moon, SlidersHorizontal, Sun, Ticket, X } from "lucide-react";

const Icons = {
  calendar: Calendar,
  close: X,
  dark: Moon,
  external: ExternalLink,
  favorite: Heart,
  filter: Funnel,
  light: Sun,
  location: MapPin,
  sliders: SlidersHorizontal,
  ticket: Ticket,
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
  color = "currentColor",
}) => {
  const IconComponent = Icons[name];
  return IconComponent ? <IconComponent size={size} color={color} /> : null;
};
