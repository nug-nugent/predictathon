import type { Meta, StoryObj } from "@storybook/react-vite";
import { nug, stu } from "../../services/user-service";
import { LoggedInHeader } from "./LoggedInHeader";

const meta = {
    title: "Layout/Logged In Header",
    component: LoggedInHeader
} satisfies Meta<typeof LoggedInHeader>;

export default meta;
type Story = StoryObj<typeof meta>;

export const LoggedInAsStu: Story = {
    parameters: { user: stu }
};

export const LoggedInAsNug: Story = {
    parameters: { user: nug }
};
