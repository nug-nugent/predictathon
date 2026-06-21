import type { Meta, StoryObj } from "@storybook/react-vite";
import { fn } from 'storybook/test';
import { nug, stu } from "../../services/user-service";
import { SiteHeader } from "./SiteHeader";

const meta = {
    title: 'Components/Site Header',
    component: SiteHeader,
    args: {
        onMenuButtonClick: fn()
    }
} satisfies Meta<typeof SiteHeader>;

export default meta;
type Story = StoryObj<typeof meta>;

export const NotLoggedIn: Story = {};

export const LoggedInAsStu: Story = {
    parameters: { user: stu }
};

export const LoggedInAsNug: Story = {
    parameters: { user: nug }
};
