# c# AssetBundle Build 工具

基于 http://blog.csdn.net/lyh916/article/details/51015156 这一版本改造，补充 5.3.x 版本的 Variant 属性，并可以自由选择平台等属性，可以自由灵活的打包 AssetBundle 并生成文件名 | MD5 格式 TXT 文件，是为临时或测试场景提供的自由打包工具。

2016.10.14：修正了由于粗心导致的缺少 SimpleContainer 文件无法独立使用的问题，现已可以独立使用，SimpleContainer 和 ToluaContainer 已经包含了该工具无需重复添加
