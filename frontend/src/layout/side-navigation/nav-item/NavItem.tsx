import { Button } from "@chakra-ui/react";
import { NavLink } from "react-router";

export function NavItem({
    to,
    icon,
    label,
    onClick
}: {
    to: string;
    icon: React.ReactNode;
    label: string;
    onClick?: () => void;
}) {
    return (
        <NavLink to={to} onClick={onClick}>
            {({ isActive }) => (
                <Button
                    variant={isActive ? "subtle" : "ghost"}
                    colorPalette="blue"
                    py={{ base: 8, lg: 6 }}
                    w={"100%"}
                    justifyContent="flex-start">
                    {icon}
                    {label}
                </Button>
            )}
        </NavLink>
    );
}
