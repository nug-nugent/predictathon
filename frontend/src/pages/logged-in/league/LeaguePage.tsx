import { Center, Spinner, Table, Text, useBreakpointValue } from "@chakra-ui/react";
import { useEffect, useState } from "react";
import { Link } from "react-router";
import { getLeague } from "../../../services/league-service";

export function LeaguePage() {
    const [items, setItems] = useState<any[]>([]);
    const hideColumns = useBreakpointValue({
        base: true,
        sm: false
    });

    useEffect(() => {
        const asyncFetch = async () => {
            const leagueData = await getLeague();
            setItems(leagueData.results);
        };
        asyncFetch();
    }, []);

    return (
        <>
            {items.length === 0 ? (
                <Center mt={4}>
                    <Spinner />
                </Center>
            ) : (
                <Table.Root size="sm" variant="line" striped showColumnBorder stickyHeader>
                    {!hideColumns ? (
                        <Table.ColumnGroup>
                            <Table.Column htmlWidth="20px" />
                            <Table.Column htmlWidth="50%" />
                            <Table.Column />
                            <Table.Column />
                            <Table.Column />
                            <Table.Column />
                            <Table.Column />
                            <Table.Column />
                            <Table.Column />
                        </Table.ColumnGroup>
                    ) : (
                        <Table.ColumnGroup>
                            <Table.Column htmlWidth="20px" />
                            <Table.Column htmlWidth="60%" />
                            <Table.Column />
                            <Table.Column />
                        </Table.ColumnGroup>
                    )}
                    <Table.Header>
                        <Table.Row>
                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>
                                POS
                            </Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>
                                NAME
                            </Table.ColumnHeader>
                            {!hideColumns && (
                                <>
                                    <Table.ColumnHeader
                                        fontWeight={"bold"}
                                        fontSize={"0.8em"}
                                        textAlign={"center"}
                                        display={{ base: "none", sm: "table-cell" }}>
                                        3
                                    </Table.ColumnHeader>
                                    <Table.ColumnHeader
                                        fontWeight={"bold"}
                                        fontSize={"0.8em"}
                                        textAlign={"center"}
                                        display={{ base: "none", sm: "table-cell" }}>
                                        2
                                    </Table.ColumnHeader>
                                    <Table.ColumnHeader
                                        fontWeight={"bold"}
                                        fontSize={"0.8em"}
                                        textAlign={"center"}
                                        display={{ base: "none", sm: "table-cell" }}>
                                        1
                                    </Table.ColumnHeader>
                                    <Table.ColumnHeader
                                        fontWeight={"bold"}
                                        fontSize={"0.8em"}
                                        textAlign={"center"}
                                        display={{ base: "none", sm: "table-cell" }}>
                                        0
                                    </Table.ColumnHeader>
                                    <Table.ColumnHeader
                                        fontWeight={"bold"}
                                        fontSize={"0.8em"}
                                        textAlign={"center"}
                                        display={{ base: "none", sm: "table-cell" }}>
                                        L
                                    </Table.ColumnHeader>
                                </>
                            )}

                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>
                                POINTS
                            </Table.ColumnHeader>
                            <Table.ColumnHeader fontWeight={"bold"} fontSize={"0.8em"} textAlign={"center"}>
                                AGD
                            </Table.ColumnHeader>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {items.map((item) => (
                            <Table.Row key={item.pos}>
                                <Table.Cell fontSize={"0.9em"} textAlign={"right"} padding={{ base: 1, md: 2 }}>
                                    {item.pos}
                                </Table.Cell>
                                <Table.Cell fontSize={"0.9em"} padding={{ base: 1, md: 2 }}>
                                    <Text truncate>
                                        <Link to={`/profile/${item.userId}`}>{item.username}</Link>
                                    </Text>
                                </Table.Cell>
                                {!hideColumns && (
                                    <>
                                        <Table.Cell
                                            fontSize={"0.9em"}
                                            textAlign={"center"}
                                            color={"points.3"}
                                            padding={{ base: 1, md: 2 }}>
                                            {item.three}
                                        </Table.Cell>
                                        <Table.Cell
                                            fontSize={"0.9em"}
                                            textAlign={"center"}
                                            color={"points.2"}
                                            padding={{ base: 1, md: 2 }}>
                                            {item.two}
                                        </Table.Cell>
                                        <Table.Cell
                                            fontSize={"0.9em"}
                                            textAlign={"center"}
                                            color={"points.1"}
                                            padding={{ base: 1, md: 2 }}>
                                            {item.one}
                                        </Table.Cell>
                                        <Table.Cell
                                            fontSize={"0.9em"}
                                            textAlign={"center"}
                                            color={"points.0"}
                                            padding={{ base: 1, md: 2 }}>
                                            {item.zero}
                                        </Table.Cell>
                                        <Table.Cell
                                            fontSize={"0.9em"}
                                            textAlign={"center"}
                                            color={"points.0"}
                                            padding={{ base: 1, md: 2 }}>
                                            {item.missed}
                                        </Table.Cell>
                                    </>
                                )}
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} padding={{ base: 1, md: 2 }}>
                                    {item.points}
                                </Table.Cell>
                                <Table.Cell fontSize={"0.9em"} textAlign={"center"} padding={{ base: 1, md: 2 }}>
                                    {item.agd}
                                </Table.Cell>
                            </Table.Row>
                        ))}
                    </Table.Body>
                </Table.Root>
            )}
        </>
    );
}
