import { Box, Heading, List} from "@chakra-ui/react";
import { ChartColumn, Dices, Home, Info, Medal, MessagesSquare, TableProperties, Trophy, Users, Wrench } from "lucide-react";
import { NavItem } from "./nav-item/NavItem";
import { UserRole, useUser } from "../../providers/UserProvider";

export function SideNavigation({ onClick }: { onClick?: () => void }) {
    const { user } = useUser();

    return (
        <Box>
            <List.Root variant="plain" fontSize={{ base: "lg", lg: "md" }} fontWeight="bold" onClick={onClick}>
                <NavItem to="/" icon={<Home size={20} />} label="Home" />
                <NavItem to="/predictions" icon={<Dices size={20} />} label="Predictions" />
                <NavItem to="/league" icon={<TableProperties size={20} />} label="League Table" />
                <NavItem to="/board" icon={<MessagesSquare size={20} />} label="Messageboard" />
                <NavItem to="/stats" icon={<ChartColumn size={20} />} label="Statistics" />
                <NavItem to="/hof" icon={<Medal size={20} />} label="Hall of Fame" />
                <NavItem to="/rules" icon={<Info size={20} />} label="Rules" />

                {user?.role === UserRole.Admin && (
                    <>
                        <Heading size={"sm"} mt={2}>Admin</Heading>
                        <NavItem to="/admin/tournaments" icon={<Trophy size={20} />} label="Tournaments" />
                        <NavItem to="/admin/process" icon={<Wrench size={20} />} label="Process Results" />
                        <NavItem to="/admin/users" icon={<Users size={20} />} label="Users" />
                    </>
                )}
            </List.Root>
        </Box>
    )
}