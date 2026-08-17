import { NavLink } from "react-router";
import { Box, HStack, List } from "@chakra-ui/react";

export function NavItem({ to, icon, label, onClick }: { to: string; icon: React.ReactNode; label: string; onClick?: () => void }) {
    return (
        <List.Item>
            {/* List.Item renders as a flex container, so the anchor needs flexGrow (not just
                display:block) to actually fill it - a block child of a flex parent still just
                shrinks to its content's width without one. */}
            <NavLink to={to} onClick={onClick} style={{ textDecoration: "none", display: "block", flexGrow: 1 }}>
                {({ isActive }) => (
                    // as="span": the NavLink anchor is the interactive element; rendering a real
                    // <button> inside it is invalid HTML and double-stops keyboard tabbing. The
                    // span just carries the row styling.
                    <HStack
                        as="span"
                        gap={3}
                        py={{ base: 3, lg: 2 }}
                        pl="13px"
                        pr={4}
                        width="100%"
                        borderLeftWidth="3px"
                        borderLeftColor={isActive ? "brand.accent" : "transparent"}
                        bg={isActive ? "nav.activeTint" : "transparent"}
                        _hover={{ bg: isActive ? "nav.activeTint" : "bg.muted" }}
                    >
                        <Box
                            boxSize="26px"
                            flexShrink={0}
                            borderRadius="7px"
                            bg={isActive ? "brand.accent" : "transparent"}
                            color={isActive ? "nav.activeFg" : "nav.fg"}
                            opacity={isActive ? 1 : 0.7}
                            display="flex"
                            alignItems="center"
                            justifyContent="center"
                        >
                            {icon}
                        </Box>
                        <Box
                            fontFamily="body"
                            fontSize={{ base: "lg", lg: "md" }}
                            fontWeight={isActive ? "bold" : "medium"}
                            color={isActive ? "brand.accent" : "nav.fg"}
                        >
                            {label}
                        </Box>
                    </HStack>
                )}
            </NavLink>
        </List.Item>
    )
}
