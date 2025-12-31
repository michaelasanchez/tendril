import { Calendar, ExternalLink, Funnel, MapPin, Ticket } from "lucide-react";

const Icons = {
  calendar: Calendar,
  external: ExternalLink,
  filter: Funnel,
  location: MapPin,
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
