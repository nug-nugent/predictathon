import React, { useState } from "react";
import { icons } from "../../Modules/icons";
import { Column, Container, Footer, FooterIcon, Header, HeaderColumn, PageNumber, Row } from "./styles";

const getColumnsFromData = (data) => Object.keys(data[0]).map((prop) => ({ name: prop }));

export const Table = ({ columns, data, pageSize, groupColumn, boldCheckFn }) => {
    const [page, setPage] = useState(1);

    columns = columns || getColumnsFromData(data);
    data = data || [];
    pageSize = pageSize || 10;
    groupColumn = groupColumn || columns.find((c) => c.isGroup)?.name;

    const totalPages = Math.ceil(data?.length / pageSize);
    const pageData = data.slice((page - 1) * pageSize, page * pageSize);

    return (
        <>
            <Container>
                <Header>
                    {columns.map((col) => <HeaderColumn key={col.name} bottomBorder={true} rightBorder={col.isHeaderColumn} align={col.headerAlign || col.align} minWidth={col.minWidth} width={col.width} mobileWidth={col.mobileWidth}>{col.title || col.name.toString().toUpperCase()}</HeaderColumn>)}
                </Header>
                {pageData.map((row, index) => {
                    const lastInGroup = groupColumn && index !== (pageData.length - 1) && row[groupColumn] !== pageData[index + 1][groupColumn];
                    const isBold = boldCheckFn && boldCheckFn(row);
                    return <Row key={index}>
                        {columns.map((col) => col.isHeaderColumn
                            ? <HeaderColumn key={col.name} align={col.headerAlign || col.align} bottomBorder={false} rightBorder={true}>{col.format ? col.format(row) : row[col.name]}</HeaderColumn>
                            : <Column key={col.name} align={col.dataAlign || col.align}
                                lastInGroup={lastInGroup} isBold={isBold}>{col.format ? col.format(row) : row[col.name]}</Column>)}
                    </Row>
                })}
            </Container>
            {totalPages > 1 && (
                <Footer>
                    <FooterIcon icon={icons.arrowLeft} disabled={page === 1} onClick={() => page > 1 && setPage(page - 1)} />
                    {[...Array(totalPages).keys()].map((_, i) => ++i).map((i) => <PageNumber key={i} selected={page === i} onClick={() => setPage(i)}>{i}</PageNumber>)}
                    <FooterIcon icon={icons.arrowRight} disabled={page === totalPages} onClick={() => page < totalPages && setPage(page + 1)}  />
                </Footer>
            )}
        </>
    );
}
