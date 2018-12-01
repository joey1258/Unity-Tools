# c# Unity 工具集

AddBuildMapWindow.cs 为 c# AssetBundle Build ，基于 http://blog.csdn.net/lyh916/article/details/51015156 这一版本改造，补充 5.3.x 版本的 Variant 属性，并可以自由选择平台等属性，可以自由灵活的打包 AssetBundle 并生成文件名 | MD5 格式 TXT 文件，是为临时或测试场景提供的自由打包工具。

IOUtils.cs 为 IO 工具，负责读写本地文件，包括从指定路径删除一个文件、从指定路径文件中读取一个字符串、在指定子文件夹不存在的情况下在路径下创建一个子文件夹、删除指定文件夹及其下所有文件、返回指定路径下的所有文件名等功能
