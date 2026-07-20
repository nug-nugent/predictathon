import { NavLink } from "react-router";
import { Button, List } from "@chakra-ui/react";

export function NavItem({ to, icon, label, onClick }: { to: string; icon: React.ReactNode; label: string; onClick?: () => void }) {
    return (
        <List.Item>
            <NavLink to={to} onClick={onClick}>
                {({ isActive }) => (
                    // as="span": the NavLink anchor is the interactive element; rendering a real
                    // <button> inside it is invalid HTML and double-stops keyboard tabbing. The
                    // span just carries the button styling.
                    <Button
                        as="span"
                        variant="ghost"
                        justifyContent="flex-start"
                        py={{ base: 8, lg: 6 }}
                        width={{ base: "220px", lg: "160px" }}
                        fontFamily="body"
                        bg={isActive ? "nav.activeBg" : "transparent"}
                        color={isActive ? "nav.activeFg" : "nav.fg"}
                        fontWeight={isActive ? "bold" : "medium"}
                        _hover={{ bg: isActive ? "nav.activeBg" : "bg.muted" }}
                    >
                        {icon}
                        {label}
                    </Button>
                )}
            </NavLink>
        </List.Item>
    )
}