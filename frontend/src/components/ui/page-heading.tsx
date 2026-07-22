import { Heading, type HeadingProps } from "@chakra-ui/react";

// On desktop the always-visible side nav already highlights the current page, so repeating its
// label here would be redundant - shown only on mobile, where that nav lives behind a drawer.
export function PageHeading(props: HeadingProps) {
    return <Heading size="lg" display={{ base: "block", lg: "none" }} {...props} />;
}
