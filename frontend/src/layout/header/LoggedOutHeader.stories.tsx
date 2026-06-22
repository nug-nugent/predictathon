import type { Meta, StoryObj } from "@storybook/react-vite";
import { LoggedOutHeader as Component } from "./LoggedOutHeader";

const meta = {
    title: "Layout/Logged Out Header",
    component: Component
} satisfies Meta<typeof Component>;

export default meta;
type Story = StoryObj<typeof meta>;

export const LoggedOutHeader: Story = {};
