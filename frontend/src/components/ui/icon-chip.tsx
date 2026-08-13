import { Box, type BoxProps } from "@chakra-ui/react";
import type { LucideIcon } from "lucide-react";

type IconChipProps = BoxProps & {
    icon: LucideIcon;
    color: string;
};

// Small tinted icon badge used beside Home dashboard card headings - background is a 14% tint
// of `color`, via Chakra's color-mix opacity modifier (works for theme tokens and raw hex alike).
export function IconChip({ icon: Icon, color, ...props }: IconChipProps) {
    return (
        <Box
            boxSize="30px"
            borderRadius="8px"
            bg={`${color}/14`}
            display="flex"
            alignItems="center"
            justifyContent="center"
            flexShrink={0}
            {...props}
        >
            <Icon size={16} color={color} />
        </Box>
    );
}
