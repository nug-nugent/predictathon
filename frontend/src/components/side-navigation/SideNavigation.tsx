import { Box, List, Text } from "@chakra-ui/react";
import { CalendarPlus, ChartColumn, Dices, Home, Info, ListChecks, Medal, Megaphone, MessagesSquare, TableProperties, Ticket, TriangleAlert, Trophy, Users, Wrench } from "lucide-react";
import { NavItem } from "./nav-item/NavItem";
import { useUser } from "../../hooks/useUser";
import { useCompetition } from "../../hooks/useCompetition";
import { Role } from "../../constants/roles";

export function SideNavigation({ onClick }: { onClick?: () => void }) {
    const { user } = useUser();
    const roles = user?.roles ?? [];
    const { competitions, isLoading: competitionsLoading } = useCompetition();
    // Admins are exempt from the "register for a competition first" gate (see ProtectedRoute),
    // so their links stay visible even with zero registrations. While loading, assume they'll
    // have competitions to avoid a flash of a near-empty menu.
    const showCompetitionLinks = roles.length > 0 || competitionsLoading || competitions.length > 0;

    return (
        <Box>
            <List.Root variant="plain" fontSize={{ base: "lg", lg: "md" }} fontWeight="bold" onClick={onClick}>
                <NavItem to="/" icon={<Home size={20} />} label="Home" />
                {showCompetitionLinks && (
                    <>
                        <NavItem to="/predictions" icon={<Dices size={20} />} label="Predictions" />
                        <NavItem to="/league" icon={<TableProperties size={20} />} label="League Table" />
                        <NavItem to="/board" icon={<MessagesSquare size={20} />} label="Messageboard" />
                        <NavItem to="/stats" icon={<ChartColumn size={20} />} label="Statistics" />
                        <NavItem to="/hof" icon={<Medal size={20} />} label="Hall of Fame" />
                        <NavItem to="/rules" icon={<Info size={20} />} label="Rules" />
                    </>
                )}

                {roles.length > 0 && (
                    <>
                        <Text mt={4} pt={4} mb={1} px={3} borderTopWidth="1px" borderTopColor="border.hairline"
                            fontSize="11px" fontWeight="bold" textTransform="uppercase"
                            letterSpacing="wide" color="nav.sectionLabel">Admin</Text>
                        {roles.includes(Role.CompetitionAdministrator) && (
                            <NavItem to="/admin/tournaments" icon={<Trophy size={20} />} label="Tournaments" />
                        )}
                        {roles.includes(Role.MatchAdministrator) && (
                            <NavItem to="/admin/matches" icon={<CalendarPlus size={20} />} label="Matches" />
                        )}
                        {roles.includes(Role.MatchAdministrator) && (
                            <NavItem to="/admin/process" icon={<Wrench size={20} />} label="Process Results" />
                        )}
                        {roles.includes(Role.MatchAdministrator) && (
                            <NavItem to="/admin/fixture-changes" icon={<ListChecks size={20} />} label="Fixture Changes" />
                        )}
                        {roles.includes(Role.UserAdministrator) && (
                            <NavItem to="/admin/users" icon={<Users size={20} />} label="Users" />
                        )}
                        {roles.includes(Role.UserAdministrator) && (
                            <NavItem to="/admin/payment-credits" icon={<Ticket size={20} />} label="Payment Credits" />
                        )}
                        {roles.includes(Role.UserAdministrator) && (
                            <NavItem to="/admin/errors" icon={<TriangleAlert size={20} />} label="Error Log" />
                        )}
                        {roles.includes(Role.UserAdministrator) && (
                            <NavItem to="/admin/announcements" icon={<Megaphone size={20} />} label="Announcements" />
                        )}
                    </>
                )}
            </List.Root>
        </Box>
    )
}