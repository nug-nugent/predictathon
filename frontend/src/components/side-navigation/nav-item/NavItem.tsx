import { NavLink } from "react-router";
import { Button, List } from "@chakra-ui/react";

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
        <List.Item>
            <NavLink to={to} onClick={onClick}>
                {({ isActive }) => (
                    <Button
                        variant={isActive ? "subtle" : "ghost"}
                        colorPalette="blue"
                        py={{ base: 8, lg: 6 }}
                        width={{ base: "220px", lg: "160px" }}
                        justifyContent="flex-start">
                        {icon}
                        {label}
                    </Button>
                )}
            </NavLink>
        </List.Item>
    );
}
