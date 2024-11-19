---
title: "Elastic Cloud"
---

The Elastic Cloud is the recommended option for both development and production. You can sign up for a free 14-day trial at https://cloud.elastic.co.

- [Sign up for a free Elastic Cloud trial](https://cloud.elastic.co/registration?onboarding_token=search&cta=cloud-registration&tech=trial&plcmt=article%20content&pg=search-labs)


## Creating a Cloud Deployment

If you just signed up for an Elastic Cloud trial account, you will be asked to create a deployment as part of your onboarding.

If you need to create a new deployment on a paid account, follow these instructions:

1. Navigate to your [Elastic Cloud](https://cloud.elastic.co/home) home page.
1. Click **Create deployment**.
1. Enter a name for your new deployment.
1. Select your preferred cloud provider and region.
1. Select your desired hardware profile and version, or use the suggested defaults if unsure.
1. Click **Create deployment**.

### Finding your Cloud ID

To authenticate against the Elasticsearch service you are going to need the Cloud ID that was assigned to your deployment.

Follow these steps to obtain your Cloud ID:

1. Navigate to your [Elastic Cloud](https://cloud.elastic.co/home) home page.
1. Locate your deployment, and click the **Manage** link under the **Actions** column.
1. The Cloud ID is displayed on the right side of the page. See the screenshot below as a reference.

## Creating an API Key

For security purposes, it is recommended that you create an API Key to use when authenticating to the Elasticsearch service.

Follow these steps to create an API Key:

1. Navigate to your [Elastic Cloud](https://cloud.elastic.co/home) home page.
1. Locate your deployment, and click the **Open** link under the **Actions** column.
1. On the left-side menu bar, click on **Stack Management** under **Management**.
1. Open **API keys** under **Security**.
1. Click **Create an API key**.
1. Give a name to the API key and click **Create API key**.
1. Copy your `encoded` API key to the clipboard, and paste it on a local text document for safe keeping until you need to use it.