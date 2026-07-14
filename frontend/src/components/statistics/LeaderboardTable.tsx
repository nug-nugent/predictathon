import { Heading, Link, Table, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import type { ReactNode } from "react";

type LeaderboardItem = { userID: string; username: string };

export function LeaderboardTable<T extends LeaderboardItem>({ title, items, columns }: {
    title: string;
    items: T[];
    columns: { header: string; render: (item: T) => ReactNode }[];
}) {
    return (
        <VStack align="stretch" gap={1}>
            <Heading size="sm">{title}</Heading>
            <Table.Root size="sm" variant="line">
                <Table.Header>
                    <Table.Row>
                        <Table.ColumnHeader>User</Table.ColumnHeader>
                        {columns.map((c) => <Table.ColumnHeader key={c.header} textAlign="center">{c.header}</Table.ColumnHeader>)}
                    </Table.Row>
                </Table.Header>
                <Table.Body>
                    {items.length === 0 ? (
                        <Table.Row>
                            <Table.Cell colSpan={columns.length + 1}>
                                <Text color="fg.muted">No records found</Text>
                            </Table.Cell>
                        </Table.Row>
                    ) : items.map((item) => (
                        <Table.Row key={item.userID}>
                            <Table.Cell>
                                <Link asChild><RouterLink to={`/profile/${item.userID}`}>{item.username}</RouterLink></Link>
                            </Table.Cell>
                            {columns.map((c) => <Table.Cell key={c.header} textAlign="center">{c.render(item)}</Table.Cell>)}
                        </Table.Row>
                    ))}
                </Table.Body>
            </Table.Root>
        </VStack>
    );
}
