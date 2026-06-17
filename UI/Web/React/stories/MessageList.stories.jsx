import { addMinutes, addSeconds } from 'date-fns';
import { MessageList } from '../Pages/MessageList/MessageList';

const reactions = [
    {"Name":"rolling on the floor laughing","Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f923.svg","Username":"stu with a long username","IsMe":false},
    {"Name":"brewdog","Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png","Username":"stu with a long username","IsMe":false},
    {"Name":"brewdog","Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png","Username":"Nugsson","IsMe":false},
    {"Name":"guinness","Url":"https://www.predictathon.co.uk/Images/Message/Reactions/pt.png","Username":"Nugsson","IsMe":false},
    {"Name":"beach with umbrella","Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f3d6.svg","Username":"Nugsson","IsMe":false},
    {"Name":"safety vest","Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f9ba.svg","Username":"3rd User","IsMe":true}];

const stuPost = {
    authorImageUrl: "https://www.predictathon.co.uk/Uploads/Images/13473b15-dc61-437c-833a-af2b987b67ef_sm.jpg",
    authorUrl: "https://www.predictathon.co.uk/Pages/User/UserDetail.aspx?UserID=13473b15-dc61-437c-833a-af2b987b67ef",
    authorName: "stu with a long username"
};

const nugPost = {
    authorImageUrl: "https://www.predictathon.co.uk/Uploads/Images/da93a123-baae-4ca4-9874-aad53feac685_sm.jpg",
    authorUrl: "https://www.predictathon.co.uk/Pages/User/UserDetail.aspx?UserID=da93a123-baae-4ca4-9874-aad53feac685",
    authorName: "Nugsson"
};

export default {
    title: "Messageboard Thread/Message List Page",
    component: MessageList,
    parameters: {
        layout: "fullscreen",
        mockData: [
            {
                url: "MessageThreadDetail.aspx?CallBack=AddReaction",
                method: 'POST',
                status: 200,
                delay: 200,
                response: reactions,
            },
            {
                url: "MessageThreadDetail.aspx?CallBack=GetOlderMessages&ThreadId=:threadIdd&BeforeMessageId=:messageId",
                method: 'GET',
                status: 200,
                delay: 200,
                response: {
                    messages: [{
                        ...stuPost,
                        id: "a-really-old-post",
                        date: addMinutes(new Date(), -25),
                        text: "This is the first message in the thread.",
                    },
                    {
                        ...nugPost,
                        id: "an-old-post",
                        date: addMinutes(new Date(), -20),
                        text: "This is message number 2...",
                    }]
                }
            },
            {
                url: "MessageThreadDetail.aspx?CallBack=GetNewerMessages&ThreadId=:threadIdd&AfterMessageId=:messageId",
                method: 'GET',
                status: 200,
                delay: 200,
                response: {
                    messages: [{
                        ...stuPost,
                        id: "a-newer-post",
                        date: new Date(),
                        text: "This is the last message in the thread."
                    }]
                }
            }
        ]
    }
}

const Template = (args) => <MessageList {...args} />;
const BaseArgs = {
    title: "Message thread title",
    customReactionsPath: "https://www.predictathon.co.uk/Images/Message/Reactions",
    firstUnreadMessageId: "a-post1",
    messagesBefore: 2,
    messagesAfter: 1,
    messages: [
        {
            ...stuPost,
            id: "a-post-with-reactions",
            date: addMinutes(new Date(), -15),
            text: "Here is some **message** text with *markdown* syntax.\n\nParagraph 2 includes a URL like https://www.google.com.\n\nAnd here's a third paragraph.",
            reactions: reactions
        },
        {
            ...nugPost,
            id: "a-post1",
            date: addMinutes(new Date(), -10),
            text: "Here is a short message.",
        },
        {
            ...stuPost,
            id: "a-post2",
            date: addMinutes(new Date(), -5),
            text: "Here's a short message with lots of reactions.",
            reactions: [
                {"Name":"rolling on the floor laughing",
                "Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f923.svg",
                "Username":"stu with a long username",
                "IsMe":false
            },{"Name":"brewdog",
                "Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png",
                "Username":"stu with a long username",
                "IsMe":false
            },{"Name":"brewdog",
                "Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png",
                "Username":"Nugsson",
                "IsMe":false
            },{"Name":"pussy time",
                "Url":"https://www.predictathon.co.uk/Images/Message/Reactions/pt.png",
                "Username":"Nugsson",
                "IsMe":false
            },{"Name":"beach with umbrella",
                "Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f3d6.svg",
                "Username":"Nugsson",
                "IsMe":false
            },{"Name":"safety vest",
                "Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f9ba.svg",
                "Username":"3rd User",
                "IsMe":true
            },{"Name":"ludo",
                "Url":"https://www.predictathon.co.uk/Images/Message/Reactions/ludo.png",
                "Username":"3rd User",
                "IsMe":true
            },{"Name":"world map",
                "Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f5fa.svg",
                "Username":"3rd User",
                "IsMe":true
            },{"Name":"pirate flag",
                "Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f3f4-200d-2620-fe0f.svg",
                "Username":"3rd User","IsMe":true
            },{"Name":"flag: Denmark",
                "Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f1e9-1f1f0.svg",
                "Username":"3rd User","IsMe":true
            },{"Name":"brewdog",
                "Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png",
                "Username":"3rd User","IsMe":true
            }]
        },
        {
            ...nugPost,
            id: "portrait-image-post",
            date: addMinutes(new Date(), -4),
            text: "This post has a portrait image\n\nIt'll be interested to see how it handles wrapping of long paragraph of text when there's an image and whatnot...\n\nAnd here's a third paragraph.",
            imageUrl: "https://www.predictathon.co.uk/Uploads/Images/Message/4016c1b7-b0dc-48fd-b1a3-09fd0d055424.jpg",
            smallImageUrl: "https://www.predictathon.co.uk/Uploads/Images/Message/4016c1b7-b0dc-48fd-b1a3-09fd0d055424_sm.jpg"
        },
        {
            ...stuPost,
            id: "landscape-image-post",
            date: addSeconds(new Date(), -30),
            text: "This post has a landscape image\n\nIt'll be interested to see how it handles wrapping of long paragraph of text when there's an image and whatnot...\n\nAnd here's a third paragraph.",
            imageUrl: "https://www.predictathon.co.uk/Uploads/Images/Message/e386267a-845f-4e3f-b16e-81f8f8a7f127.jpg",
            smallImageUrl: "https://www.predictathon.co.uk/Uploads/Images/Message/e386267a-845f-4e3f-b16e-81f8f8a7f127_sm.jpg"
        },
        {
            ...nugPost,
            id: "a-video-post",
            date: addSeconds(new Date(), -5),
            text: "This post has a video\n\nIt'll be interested to see how it handles wrapping of long paragraph of text when there's a video and whatnot...\n\nAnd here's a third paragraph.",
            youTubeVideoId: "45NOP1OA-EQ"
        },
        {
            ...stuPost,
            id: "a-markdown-images-post",
            date: addSeconds(new Date(), -3),
            text: "This has some inline images added via markdown.\n\n![alt text](https://www.predictathon.co.uk/Uploads/Images/Message/e386267a-845f-4e3f-b16e-81f8f8a7f127.jpg)\n\nAnd here's more text.  And another image...\n\n![alt text](https://www.predictathon.co.uk/Uploads/Images/Message/4016c1b7-b0dc-48fd-b1a3-09fd0d055424.jpg)\n\nAnd some final text."
        }
    ]
};

export const ExamplePage = Template.bind({});
ExamplePage.args = BaseArgs;
