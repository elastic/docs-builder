# Contribute on the web

This document will help you understand how to update and contribute to our documentation post-migration.

## **Overview**

Due to recent changes, we are migrating our documentation for 9.0 and forward to a new build system and new information architecture. This migration involves significant restructuring, and as a result, content may be moved and updated in multiple locations. This guide will help you navigate these changes and ensure that your contributions are made correctly.

## **Updating Documentation**

### **Update content for Version 8.x and earlier** [update-content-for-version-8.x-and-earlier]

For content that is related to 8.x, these changes should be done in the original source folders in their respective repositories. Here’s how you can do it:

1. Navigate to the page that is impacted  
2. Click the **edit** button  
3. Ensure the targeted branch is \<insert proper branch\>  
4. Make the necessary updates.  
5. Commit your changes and create a pull request.  
6. Add the appropriate labels per repo as found at [Page: Working across docs repos](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/61604182/Working+across+docs+repos)

Note: If you are working in a repo like Kibana or the cloud repo where backports can be complicated. You can use the [backport tool](https://github.com/sorenlouv/backport) to manage your backport.

### **Update content for Version 9.0 and later** [update-content-for-version-9.0-and-later]

For content related to version 9.0 and future versions, updates should be made in the [`docs-content`](https://github.com/elastic/docs-content)  repository. Follow these steps to ensure your contributions are correctly made:

1. Navigate to the page that is impacted  
2. Click the **Edit** button.  
3. Identify the section that requires updates.  
4. Make the necessary updates.  
5. Commit your changes and create a pull request.

## **Migration Considerations**

During the migration, content may be moved around, and there won't be a 1-to-1 mapping between old and new locations. This means updates may need to be applied in multiple places. Here are some important considerations:

* **Identify Changes:** Check if your update involves content that has been moved during the migration.  
* **Merge Option:** If your changes affect version 8.x content, consider merging those changes and creating an issue to update them in the [`docs-content`](https://github.com/elastic/docs-content) repository for 9.0 and later.  
* **Create an Issue:** Ensure that any changes made to version 8.x content are also reflected in the [`docs-content`](https://github.com/elastic/docs-content) repository. Create an issue to track these updates and ensure consistency across versions.

## **What if I need to update both 8.x and 9.x docs?**

If you need to merge changes that relate to version 8.x to the 9.0 and later documentation it is recommended to update the 9.x documentation first in markdown. Then you can convert the updates to asciidoc and make the changes to the 8.x documentation. To do this, follow these steps:

1. Install [pandoc](https://pandoc.org/installing.html) to convert your markdown file to asciidoc  
2. Update the content 9.x first in markdown as described in [Version 9.0 and Later](#update-content-for-version-9.0-and-later) in the [`docs-content`](https://github.com/elastic/docs-content) repository  
3. Run your changes through pandoc  
   1. If you need to bring over the entire file you can run the following command and it will create an asciidoc file for you. `pandoc -f gfm -t asciidoc ./<file-name>.md -o <file-name>.asciidoc`  
   2. If you just need to port a specific section you can use: `pandoc -f gfm -t asciidoc ./<file-name>.md` and the output of the file will be in your command window from which you can copy.  
4. Follow the steps in [Update content for Version 8.x and earlier](#update-content-for-version-8.x-and-earlier) to publish your changes.   
5. If the change is too large or complicated, create a new issue in the [`docs-content`](https://github.com/elastic/docs-content) repository detailing the changes made for the team to triage.  
6. Merge the changes and close the issue once the updates are reflected in the [`docs-content`](https://github.com/elastic/docs-content) repository.

% To be added “Kibana and Cloud repository instructions. You need to target main to backport. Changes in main will be lost and you need to recreate it in the new architecture after the freeze is over.

% Need a good category mapping across the content .. not to the file level for area level
